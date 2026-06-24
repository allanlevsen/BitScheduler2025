import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostListener,
  forwardRef,
  inject,
  input,
  output,
  signal
} from '@angular/core';
import {
  ControlValueAccessor,
  FormControl,
  NG_VALUE_ACCESSOR,
  ReactiveFormsModule
} from '@angular/forms';
import { debounceTime, distinctUntilChanged, firstValueFrom } from 'rxjs';

import { ConfigurationDataService } from '../../data-services/configuration-data.service';
import { GoogleMapsLoaderService } from '../../core/google-maps/google-maps-loader.service';

type AddressSuggestion = {
  id: string;
  primaryText: string;
  secondaryText: string | null;
  fullText: string;
};

@Component({
  selector: 'app-address-entry',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './address-entry.component.html',
  styleUrls: ['./address-entry.component.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => AddressEntryComponent),
      multi: true
    }
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AddressEntryComponent implements ControlValueAccessor {
  public readonly id = input.required<string>();
  public readonly label = input('Address');
  public readonly placeholder = input('Start typing an address');
  public readonly helpText = input<string | null>(null);
  public readonly selectedAddress = output<string>();
  public readonly addressCommitted = output<string>();

  private readonly configurationDataService = inject(ConfigurationDataService);
  private readonly googleMapsLoaderService = inject(GoogleMapsLoaderService);
  private readonly hostElement = inject(ElementRef<HTMLElement>);

  protected readonly addressControl = new FormControl('', { nonNullable: true });
  protected readonly suggestions = signal<AddressSuggestion[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly statusMessage = signal<string | null>(null);
  protected readonly dropdownOpen = signal(false);
  protected isDisabled = false;

  private readonly optionsPromise = firstValueFrom(this.configurationDataService.getGoogleMappingConfiguration());
  private placesLibraryPromise: Promise<google.maps.PlacesLibrary | null> | null = null;
  private autocompleteSessionToken: google.maps.places.AutocompleteSessionToken | null = null;
  private currentLookupId = 0;
  private lastCommittedAddress = '';
  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  public constructor() {
    this.addressControl.valueChanges.pipe(
      debounceTime(800),
      distinctUntilChanged()
    ).subscribe((value) => {
      this.onChange(value);
      void this.lookupSuggestions(value);
    });
  }

  @HostListener('document:click', ['$event'])
  protected handleDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    if (!target || !this.hostElement.nativeElement.contains(target)) {
      this.dropdownOpen.set(false);
    }
  }

  public writeValue(value: string | null): void {
    this.addressControl.setValue(value ?? '', { emitEvent: false });
    this.suggestions.set([]);
    this.dropdownOpen.set(false);
    this.statusMessage.set(null);
    this.autocompleteSessionToken = null;
    this.lastCommittedAddress = '';
  }

  public registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  public registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  public setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;

    if (isDisabled) {
      this.addressControl.disable({ emitEvent: false });
      return;
    }

    this.addressControl.enable({ emitEvent: false });
  }

  protected focus(): void {
    if (!this.addressControl.value.trim()) {
      return;
    }

    if (this.suggestions().length > 0) {
      this.dropdownOpen.set(true);
    }
  }

  protected selectSuggestion(suggestion: AddressSuggestion): void {
    const value = suggestion.fullText;
    this.addressControl.setValue(value, { emitEvent: false });
    this.suggestions.set([]);
    this.dropdownOpen.set(false);
    this.statusMessage.set(null);
    this.autocompleteSessionToken = null;
    this.onChange(value);
    this.lastCommittedAddress = normalizeAddress(value);
    this.selectedAddress.emit(value);
    this.addressCommitted.emit(value);
  }

  protected markTouched(): void {
    const value = normalizeAddress(this.addressControl.value);
    if (value && value !== this.lastCommittedAddress) {
      this.lastCommittedAddress = value;
      this.addressCommitted.emit(value);
    }

    this.onTouched();
  }

  private async lookupSuggestions(value: string): Promise<void> {
    const query = value.trim();

    if (!query) {
      this.suggestions.set([]);
      this.dropdownOpen.set(false);
      this.statusMessage.set(null);
      return;
    }

    if (query.length < 3) {
      this.suggestions.set([]);
      this.dropdownOpen.set(false);
      this.statusMessage.set('Type at least 3 characters to search for addresses.');
      return;
    }

    const lookupId = ++this.currentLookupId;
    this.isLoading.set(true);
    this.statusMessage.set(null);

    try {
      const placesLibrary = await this.getPlacesLibrary();
      const predictions = await this.fetchSuggestions(placesLibrary, query);

      if (lookupId !== this.currentLookupId) {
        return;
      }

      this.suggestions.set(predictions);
      this.dropdownOpen.set(predictions.length > 0);
      this.statusMessage.set(predictions.length > 0 ? null : 'No matching addresses found.');
    } catch (error: unknown) {
      console.error('Failed to load address suggestions.', error);

      if (lookupId === this.currentLookupId) {
        this.suggestions.set([]);
        this.dropdownOpen.set(false);
        this.statusMessage.set('Unable to load address suggestions right now.');
      }
    } finally {
      if (lookupId === this.currentLookupId) {
        this.isLoading.set(false);
      }
    }
  }

  private async fetchSuggestions(
    placesLibrary: google.maps.PlacesLibrary | null,
    query: string
  ): Promise<AddressSuggestion[]> {
    const sessionToken = this.getOrCreateAutocompleteSessionToken(placesLibrary);

    if (placesLibrary?.AutocompleteSuggestion?.fetchAutocompleteSuggestions) {
      const response = await placesLibrary.AutocompleteSuggestion.fetchAutocompleteSuggestions({
        input: query,
        ...(sessionToken ? { sessionToken } : {})
      });

      return response.suggestions
        .map((suggestion) => suggestion.placePrediction)
        .filter((prediction): prediction is google.maps.places.PlacePrediction => prediction !== null)
        .map((prediction) => ({
          id: prediction.placeId,
          primaryText: prediction.mainText?.toString() ?? prediction.text.toString(),
          secondaryText: prediction.secondaryText?.toString() ?? null,
          fullText: prediction.text.toString()
        }));
    }

    const autocompleteServiceConstructor =
      placesLibrary?.AutocompleteService ??
      google.maps.places?.AutocompleteService;

    if (autocompleteServiceConstructor) {
      const service = new autocompleteServiceConstructor();
      const response = await service.getPlacePredictions({
        input: query,
        ...(sessionToken ? { sessionToken } : {})
      });

      return response.predictions.map((prediction) => ({
        id: prediction.place_id,
        primaryText: prediction.structured_formatting.main_text,
        secondaryText: prediction.structured_formatting.secondary_text ?? null,
        fullText: prediction.description
      }));
    }

    throw new Error('The Google Places library did not expose an autocomplete API.');
  }

  private getOrCreateAutocompleteSessionToken(
    placesLibrary: google.maps.PlacesLibrary | null
  ): google.maps.places.AutocompleteSessionToken | null {
    if (this.autocompleteSessionToken) {
      return this.autocompleteSessionToken;
    }

    const sessionTokenConstructor =
      placesLibrary?.AutocompleteSessionToken ??
      google.maps.places?.AutocompleteSessionToken;

    if (!sessionTokenConstructor) {
      return null;
    }

    this.autocompleteSessionToken = new sessionTokenConstructor();
    return this.autocompleteSessionToken;
  }

  private async getPlacesLibrary(): Promise<google.maps.PlacesLibrary | null> {
    this.placesLibraryPromise ??= (async () => {
      const configuration = await this.optionsPromise;
      await this.googleMapsLoaderService.load(configuration);

      if (typeof google.maps.importLibrary === 'function') {
        return await google.maps.importLibrary('places') as google.maps.PlacesLibrary;
      }

      return google.maps.places
        ? google.maps.places as unknown as google.maps.PlacesLibrary
        : null;
    })();

    return this.placesLibraryPromise;
  }
}

function normalizeAddress(value: string | null | undefined): string {
  return value?.trim() ?? '';
}

import { CommonModule } from '@angular/common';
import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, forkJoin, of } from 'rxjs';

import { EventModel, EventRequest } from '../models/event.models';
import { ResourceListItem } from '../../resources/models/resource.models';
import { LocationDataService } from '../../../data-services/location-data.service';
import { AddressEntryComponent } from '../../../shared/address-entry/address-entry.component';

@Component({
  selector: 'app-event-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AddressEntryComponent],
  templateUrl: './event-form.component.html',
  styleUrls: ['./event-form.component.css']
})
export class EventFormComponent {
  public readonly title = input('Event');
  public readonly submitLabel = input('Save Event');
  public readonly resources = input<ResourceListItem[]>([]);
  public readonly initialValue = input<EventModel | null>(null);
  public readonly saving = input(false);
  public readonly errorMessage = input<string | null>(null);

  public readonly save = output<EventRequest>();
  public readonly cancel = output<void>();

  private readonly formBuilder = new FormBuilder();
  private readonly locationDataService = inject(LocationDataService);

  protected readonly startLocationMessage = signal<string | null>(null);
  protected readonly endLocationMessage = signal<string | null>(null);
  protected startDateTimeSnapshot = '';

  protected readonly form = this.formBuilder.nonNullable.group({
    bitResourceId: [0, [Validators.required, Validators.min(1)]],
    eventType: [''],
    startDateTime: ['', Validators.required],
    endDateTime: ['', Validators.required],
    startAddress: [''],
    startLatitude: [''],
    startLongitude: [''],
    startHexGridId: [''],
    endAddress: [''],
    endLatitude: [''],
    endLongitude: [''],
    endHexGridId: [''],
    requiresTransportation: [false],
    requiresReturnTransportation: [false],
    reserveScheduleBits: [true],
    updatedBy: ['system', Validators.required]
  });

  public constructor() {
    effect(() => {
      const event = this.initialValue();

      if (!event) {
        this.form.reset({
          bitResourceId: 0,
          eventType: '',
          startDateTime: '',
          endDateTime: '',
          startAddress: '',
          startLatitude: '',
          startLongitude: '',
          startHexGridId: '',
          endAddress: '',
          endLatitude: '',
          endLongitude: '',
          endHexGridId: '',
          requiresTransportation: false,
          requiresReturnTransportation: false,
          reserveScheduleBits: true,
          updatedBy: 'system'
        }, { emitEvent: false });
        this.startLocationMessage.set(null);
        this.endLocationMessage.set(null);

        return;
      }

      this.form.reset({
        bitResourceId: event.bitResourceId,
        eventType: event.eventType ?? '',
        startDateTime: toDateTimeLocalValue(event.startDateTime),
        endDateTime: toDateTimeLocalValue(event.endDateTime),
        startAddress: event.startAddress ?? '',
        startLatitude: toOptionalText(event.startLatitude),
        startLongitude: toOptionalText(event.startLongitude),
        startHexGridId: toOptionalText(event.startHexGridId),
        endAddress: event.endAddress ?? '',
        endLatitude: toOptionalText(event.endLatitude),
        endLongitude: toOptionalText(event.endLongitude),
        endHexGridId: toOptionalText(event.endHexGridId),
        requiresTransportation: event.requiresTransportation,
        requiresReturnTransportation: event.requiresReturnTransportation,
        reserveScheduleBits: event.scheduleBitsReserved,
        updatedBy: event.updatedBy || 'system'
      }, { emitEvent: false });
      this.startLocationMessage.set(null);
      this.endLocationMessage.set(null);
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.save.emit({
      bitResourceId: value.bitResourceId,
      eventType: normalizeOptionalText(value.eventType),
      startDateTime: toIsoDateTime(value.startDateTime),
      endDateTime: toIsoDateTime(value.endDateTime),
      startAddress: normalizeOptionalText(value.startAddress),
      startLatitude: parseOptionalNumber(value.startLatitude),
      startLongitude: parseOptionalNumber(value.startLongitude),
      startHexGridId: parseOptionalInteger(value.startHexGridId),
      endAddress: normalizeOptionalText(value.endAddress),
      endLatitude: parseOptionalNumber(value.endLatitude),
      endLongitude: parseOptionalNumber(value.endLongitude),
      endHexGridId: parseOptionalInteger(value.endHexGridId),
      requiresTransportation: value.requiresTransportation,
      requiresReturnTransportation: value.requiresReturnTransportation,
      reserveScheduleBits: value.reserveScheduleBits,
      updatedBy: normalizeOptionalText(value.updatedBy) ?? 'system'
    });
  }

  protected captureStartDateTimeSnapshot(): void {
    this.startDateTimeSnapshot = this.form.controls.startDateTime.getRawValue();
  }

  protected syncEndDateTimeFromStart(): void {
    if (this.initialValue()) {
      return;
    }

    const startDateTime = this.form.controls.startDateTime.getRawValue();
    if (startDateTime === this.startDateTimeSnapshot || !isValidDateTimeLocalValue(startDateTime)) {
      return;
    }

    this.form.patchValue({
      endDateTime: addHoursToDateTimeLocalValue(startDateTime, 1)
    });

    this.startDateTimeSnapshot = startDateTime;
  }

  protected resolveSelectedAddress(kind: 'start' | 'end', address: string): void {
    const normalizedAddress = address.trim();
    if (!normalizedAddress) {
      return;
    }

    this.setLocationMessage(kind, null);
    this.patchResolvedLocation(kind, null);

    forkJoin({
      geocoded: this.locationDataService.geocodeAddress(normalizedAddress).pipe(catchError(() => of(null))),
      hexGrid: this.locationDataService.resolveHexGrid(normalizedAddress).pipe(catchError(() => of(null)))
    }).subscribe({
      next: ({ geocoded, hexGrid }) => {
        if (!geocoded && !hexGrid) {
          this.setLocationMessage(kind, `Unable to resolve the ${kind} address to coordinates or a hex grid id.`);
          return;
        }

        this.patchResolvedLocation(kind, {
          latitude: geocoded?.latitude ?? hexGrid?.latitude ?? null,
          longitude: geocoded?.longitude ?? hexGrid?.longitude ?? null,
          hexGridId: hexGrid?.hexGridId ?? null
        });

        if (geocoded && !hexGrid) {
          this.setLocationMessage(kind, `Coordinates were found for the ${kind} address, but the hex grid id could not be determined.`);
        } else if (!geocoded && hexGrid) {
          this.setLocationMessage(kind, `A hex grid id was found for the ${kind} address, but coordinates could not be resolved.`);
        }
      },
      error: () => {
        this.setLocationMessage(kind, `Unable to resolve the ${kind} address right now.`);
      }
    });
  }

  private setLocationMessage(kind: 'start' | 'end', message: string | null): void {
    if (kind === 'start') {
      this.startLocationMessage.set(message);
      return;
    }

    this.endLocationMessage.set(message);
  }

  private patchResolvedLocation(
    kind: 'start' | 'end',
    location: { latitude: number | null; longitude: number | null; hexGridId?: number | null } | null
  ): void {
    const targetPrefix = kind === 'start' ? 'start' : 'end';

    this.form.patchValue({
      [`${targetPrefix}Latitude`]: location?.latitude ?? null,
      [`${targetPrefix}Longitude`]: location?.longitude ?? null,
      [`${targetPrefix}HexGridId`]: location?.hexGridId ?? null
    }, { emitEvent: false });
  }
}

function toDateTimeLocalValue(value: string): string {
  const date = new Date(value);
  const timezoneOffset = date.getTimezoneOffset();
  const localDate = new Date(date.getTime() - timezoneOffset * 60000);
  return localDate.toISOString().slice(0, 16);
}

function toIsoDateTime(value: string): string {
  return new Date(value).toISOString();
}

function isValidDateTimeLocalValue(value: string): boolean {
  return value.trim().length > 0 && !Number.isNaN(new Date(value).getTime());
}

function addHoursToDateTimeLocalValue(value: string, hours: number): string {
  const date = new Date(value);
  date.setHours(date.getHours() + hours);
  return toLocalDateTimeInputValue(date);
}

function toLocalDateTimeInputValue(value: Date): string {
  const timezoneOffset = value.getTimezoneOffset();
  const localDate = new Date(value.getTime() - timezoneOffset * 60000);
  return localDate.toISOString().slice(0, 16);
}

function parseOptionalNumber(value: string | number | null | undefined): number | null {
  const normalized = normalizeOptionalText(value);
  if (normalized === null) {
    return null;
  }

  const parsed = Number(normalized);
  return Number.isNaN(parsed) ? null : parsed;
}

function parseOptionalInteger(value: string | number | null | undefined): number | null {
  const normalized = normalizeOptionalText(value);
  if (normalized === null) {
    return null;
  }

  const parsed = Number.parseInt(normalized, 10);
  return Number.isNaN(parsed) ? null : parsed;
}

function normalizeOptionalText(value: string | number | null | undefined): string | null {
  if (value === null || value === undefined) {
    return null;
  }

  const trimmed = String(value).trim();
  return trimmed.length > 0 ? trimmed : null;
}

function toOptionalText(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : String(value);
}

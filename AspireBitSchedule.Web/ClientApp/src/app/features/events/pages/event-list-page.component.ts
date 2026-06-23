import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { ClientContextService } from '../../../core/client-context/client-context.service';
import { EventDataService } from '../../../data-services/event-data.service';
import { ResourceDataService } from '../../../data-services/resource-data.service';
import { EventListRequest, EventModel } from '../models/event.models';
import { ResourceListItem } from '../../resources/models/resource.models';

@Component({
  selector: 'app-event-list-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, DatePipe],
  templateUrl: './event-list-page.component.html',
  styleUrls: ['./event-list-page.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EventListPageComponent {
  private readonly clientContext = inject(ClientContextService);
  private readonly eventDataService = inject(EventDataService);
  private readonly resourceDataService = inject(ResourceDataService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly resources = signal<ResourceListItem[]>([]);
  protected readonly events = signal<EventModel[]>([]);
  protected readonly availableEventTypes = signal<string[]>([]);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly selectedResourceIds = signal<number[]>([]);
  protected readonly selectedEventTypes = signal<string[]>([]);

  protected readonly filterForm = this.formBuilder.nonNullable.group({
    rangeStart: [''],
    rangeEnd: ['']
  });

  protected readonly eventCount = computed(() => this.events().length);
  protected readonly transportationCount = computed(() => this.events().filter((event) => event.requiresTransportation).length);
  protected readonly reservedCount = computed(() => this.events().filter((event) => event.scheduleBitsReserved).length);

  public constructor() {
    this.loadPageData();
    this.clientContext.clientChanged$
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.selectedResourceIds.set([]);
        this.selectedEventTypes.set([]);
        this.filterForm.reset({
          rangeStart: '',
          rangeEnd: ''
        });
        this.loadPageData();
      });
  }

  protected applyFilters(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const request = this.buildFilterRequest();

    const load = hasActiveFilters(request)
      ? this.eventDataService.listEvents(request)
      : this.eventDataService.listEvents();

    load.subscribe({
      next: (events) => {
        this.events.set(events);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to filter events.');
        this.loading.set(false);
      }
    });
  }

  protected resetFilters(): void {
    this.filterForm.reset({
      rangeStart: '',
      rangeEnd: ''
    });

    this.selectedResourceIds.set([]);
    this.selectedEventTypes.set([]);
    this.applyFilters();
  }

  protected toggleResource(resourceId: number, selected: boolean): void {
    this.selectedResourceIds.update((resourceIds) => selected
      ? [...resourceIds, resourceId].sort((left, right) => left - right)
      : resourceIds.filter((id) => id !== resourceId));
  }

  protected toggleEventType(eventType: string, selected: boolean): void {
    this.selectedEventTypes.update((eventTypes) => selected
      ? [...eventTypes, eventType].sort((left, right) => left.localeCompare(right))
      : eventTypes.filter((item) => item !== eventType));
  }

  protected isResourceSelected(resourceId: number): boolean {
    return this.selectedResourceIds().includes(resourceId);
  }

  protected isEventTypeSelected(eventType: string): boolean {
    return this.selectedEventTypes().includes(eventType);
  }

  protected lookupResource(resourceId: number): ResourceListItem | undefined {
    return this.resources().find((resource) => resource.bitResourceId === resourceId);
  }

  private loadPageData(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.resourceDataService.listResources().subscribe({
      next: (resources) => this.resources.set(resources),
      error: () => this.errorMessage.set('Unable to load resources.')
    });

    this.eventDataService.listEvents().subscribe({
      next: (events) => {
        this.availableEventTypes.set(extractEventTypes(events));
        this.events.set(events);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load events.');
        this.loading.set(false);
      }
    });
  }

  private buildFilterRequest(): EventListRequest {
    const value = this.filterForm.getRawValue();

    return {
      bitResourceIds: this.selectedResourceIds().length > 0 ? this.selectedResourceIds() : null,
      rangeStart: value.rangeStart ? toIsoDateTime(value.rangeStart) : null,
      rangeEnd: value.rangeEnd ? toIsoDateTime(value.rangeEnd) : null,
      eventTypes: this.selectedEventTypes().length > 0 ? this.selectedEventTypes() : null
    };
  }
}

function toIsoDateTime(value: string): string {
  return new Date(value).toISOString();
}

function extractEventTypes(events: EventModel[]): string[] {
  return Array.from(new Set(
    events
      .map((event) => event.eventType?.trim())
      .filter((eventType): eventType is string => !!eventType)
  )).sort((left, right) => left.localeCompare(right));
}

function hasActiveFilters(request: EventListRequest): boolean {
  return Boolean(
    request.rangeStart ||
    request.rangeEnd ||
    request.bitResourceIds?.length ||
    request.eventTypes?.length
  );
}

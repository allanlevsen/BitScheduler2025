import { CommonModule } from '@angular/common';
import { Component, effect, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { EventModel, EventRequest } from '../models/event.models';
import { ResourceListItem } from '../../resources/models/resource.models';

@Component({
  selector: 'app-event-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
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
        });

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
      });
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

function parseOptionalNumber(value: string): number | null {
  const normalized = normalizeOptionalText(value);
  return normalized === null ? null : Number(normalized);
}

function parseOptionalInteger(value: string): number | null {
  const normalized = normalizeOptionalText(value);
  return normalized === null ? null : Number.parseInt(normalized, 10);
}

function normalizeOptionalText(value: string | null | undefined): string | null {
  if (value === null || value === undefined) {
    return null;
  }

  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function toOptionalText(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : String(value);
}

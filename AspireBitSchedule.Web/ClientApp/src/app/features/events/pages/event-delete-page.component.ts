import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ClientContextService } from '../../../core/client-context/client-context.service';
import { EventDataService } from '../../../data-services/event-data.service';
import { EventModel } from '../models/event.models';

@Component({
  selector: 'app-event-delete-page',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
  templateUrl: './event-delete-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EventDeletePageComponent {
  private readonly clientContext = inject(ClientContextService);
  private readonly eventDataService = inject(EventDataService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly event = signal<EventModel | null>(null);
  protected readonly loading = signal(true);
  protected readonly deleting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly bitEventId = Number(this.route.snapshot.paramMap.get('bitEventId'));

  public constructor() {
    this.loadEvent();
    this.clientContext.clientChanged$
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.loadEvent());
  }

  protected deleteEvent(): void {
    this.deleting.set(true);
    this.errorMessage.set(null);

    this.eventDataService.deleteEvent(this.bitEventId).subscribe({
      next: () => void this.router.navigate(['/events']),
      error: (error: { error?: string }) => {
        this.errorMessage.set(error.error ?? 'Unable to delete the event.');
        this.deleting.set(false);
      }
    });
  }

  private loadEvent(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.eventDataService.getEvent(this.bitEventId).subscribe({
      next: (event) => {
        this.event.set(event);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load the event.');
        this.loading.set(false);
      }
    });
  }
}

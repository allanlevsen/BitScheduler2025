import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ClientContextService } from '../../../core/client-context/client-context.service';
import { ClientDataService } from '../../../data-services/client-data.service';
import { ClientFormComponent } from '../components/client-form.component';
import { ClientListItem, ClientRequest } from '../models/client.models';

@Component({
  selector: 'app-client-edit-page',
  standalone: true,
  imports: [CommonModule, ClientFormComponent],
  template: `
    @if (loading()) {
      <div class="card panel-card">
        <div class="card-body p-4">Loading client details...</div>
      </div>
    } @else {
      <app-client-form
        title="Update Client"
        submitLabel="Save Changes"
        [initialValue]="client()"
        [saving]="saving()"
        [errorMessage]="errorMessage()"
        (save)="updateClient($event)"
        (cancel)="navigateBack()" />
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClientEditPageComponent {
  private readonly clientContext = inject(ClientContextService);
  private readonly clientDataService = inject(ClientDataService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly client = signal<ClientListItem | null>(null);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly bitClientId = Number(this.route.snapshot.paramMap.get('bitClientId'));

  public constructor() {
    this.loadClient();
  }

  protected updateClient(request: ClientRequest): void {
    this.saving.set(true);
    this.errorMessage.set(null);

    this.clientDataService.updateClient(this.bitClientId, request).subscribe({
      next: () => {
        void this.clientContext.reload().finally(() => void this.router.navigate(['/clients']));
      },
      error: (error: { error?: string }) => {
        this.errorMessage.set(error.error ?? 'Unable to update the client.');
        this.saving.set(false);
      }
    });
  }

  protected navigateBack(): void {
    void this.router.navigate(['/clients']);
  }

  private loadClient(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.clientDataService.getClient(this.bitClientId).subscribe({
      next: (client) => {
        this.client.set(client);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load the client.');
        this.loading.set(false);
      }
    });
  }
}

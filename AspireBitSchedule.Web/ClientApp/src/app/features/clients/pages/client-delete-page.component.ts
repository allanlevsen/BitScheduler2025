import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ClientContextService } from '../../../core/client-context/client-context.service';
import { ClientDataService } from '../../../data-services/client-data.service';
import { ClientListItem } from '../models/client.models';

@Component({
  selector: 'app-client-delete-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './client-delete-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClientDeletePageComponent {
  private readonly clientContext = inject(ClientContextService);
  private readonly clientDataService = inject(ClientDataService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly client = signal<ClientListItem | null>(null);
  protected readonly loading = signal(true);
  protected readonly deleting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly bitClientId = Number(this.route.snapshot.paramMap.get('bitClientId'));

  public constructor() {
    this.loadClient();
  }

  protected deleteClient(): void {
    this.deleting.set(true);
    this.errorMessage.set(null);

    this.clientDataService.deleteClient(this.bitClientId).subscribe({
      next: () => {
        void this.clientContext.reload().finally(() => void this.router.navigate(['/clients']));
      },
      error: (error: { error?: string }) => {
        this.errorMessage.set(error.error ?? 'Unable to delete the client.');
        this.deleting.set(false);
      }
    });
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

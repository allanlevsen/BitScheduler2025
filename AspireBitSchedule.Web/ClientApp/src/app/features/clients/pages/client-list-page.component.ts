import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ClientContextService } from '../../../core/client-context/client-context.service';
import { ClientDataService } from '../../../data-services/client-data.service';
import { ClientListItem } from '../models/client.models';

@Component({
  selector: 'app-client-list-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './client-list-page.component.html',
  styleUrls: ['./client-list-page.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClientListPageComponent {
  private readonly clientContext = inject(ClientContextService);
  private readonly clientDataService = inject(ClientDataService);

  protected readonly clients = signal<ClientListItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly clientCount = computed(() => this.clients().length);
  protected readonly selectedClientName = computed(() => this.clientContext.currentClient()?.name ?? 'None selected');

  public constructor() {
    this.loadClients();
  }

  protected isCurrentClient(bitClientId: number): boolean {
    return this.clientContext.currentClient()?.bitClientId === bitClientId;
  }

  private loadClients(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.clientDataService.listClients().subscribe({
      next: (clients) => {
        this.clients.set(clients);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Unable to load clients.');
        this.loading.set(false);
      }
    });
  }
}

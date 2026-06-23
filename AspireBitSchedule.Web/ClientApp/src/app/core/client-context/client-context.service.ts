import { Injectable, inject, signal } from '@angular/core';
import { Subject, firstValueFrom } from 'rxjs';

import { ClientDataService } from '../../data-services/client-data.service';
import { ClientListItem } from '../../features/clients/models/client.models';

@Injectable({
  providedIn: 'root'
})
export class ClientContextService {
  private readonly clientDataService = inject(ClientDataService);
  private readonly clientChangedSubject = new Subject<ClientListItem>();
  private initializePromise: Promise<void> | null = null;

  public readonly clients = signal<ClientListItem[]>([]);
  public readonly currentClient = signal<ClientListItem | null>(null);
  public readonly ready = signal(false);
  public readonly loading = signal(false);
  public readonly errorMessage = signal<string | null>(null);
  public readonly clientChanged$ = this.clientChangedSubject.asObservable();

  public initialize(): Promise<void> {
    if (this.ready()) {
      return this.reload();
    }

    return this.reload();
  }

  public reload(): Promise<void> {
    if (this.initializePromise) {
      return this.initializePromise;
    }

    this.loading.set(true);
    this.errorMessage.set(null);
    const previousClientId = this.currentClient()?.bitClientId ?? null;

    this.initializePromise = Promise.all([
      firstValueFrom(this.clientDataService.listClients()),
      firstValueFrom(this.clientDataService.getCurrentClient())
    ])
      .then(([clients, currentClient]) => {
        this.clients.set(clients);
        this.currentClient.set(currentClient);
        this.ready.set(true);

        if (currentClient.bitClientId !== previousClientId) {
          this.clientChangedSubject.next(currentClient);
        }
      })
      .catch(() => {
        this.errorMessage.set('Unable to load clients.');
        this.ready.set(false);
        throw new Error('Unable to load clients.');
      })
      .finally(() => {
        this.loading.set(false);
        this.initializePromise = null;
      });

    return this.initializePromise;
  }

  public changeClient(bitClientId: number): void {
    if (bitClientId <= 0 || this.currentClient()?.bitClientId === bitClientId) {
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);

    this.clientDataService.setCurrentClient(bitClientId).subscribe({
      next: (client) => {
        this.currentClient.set(client);
        this.loading.set(false);
        this.clientChangedSubject.next(client);
      },
      error: () => {
        this.errorMessage.set('Unable to switch clients.');
        this.loading.set(false);
      }
    });
  }
}

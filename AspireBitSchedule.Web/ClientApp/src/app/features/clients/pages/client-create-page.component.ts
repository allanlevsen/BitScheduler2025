import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { ClientContextService } from '../../../core/client-context/client-context.service';
import { ClientDataService } from '../../../data-services/client-data.service';
import { ClientFormComponent } from '../components/client-form.component';
import { ClientRequest } from '../models/client.models';

@Component({
  selector: 'app-client-create-page',
  standalone: true,
  imports: [CommonModule, ClientFormComponent],
  template: `
    <app-client-form
      title="Create Client"
      submitLabel="Create Client"
      [saving]="saving()"
      [errorMessage]="errorMessage()"
      (save)="createClient($event)"
      (cancel)="navigateBack()" />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClientCreatePageComponent {
  private readonly clientContext = inject(ClientContextService);
  private readonly clientDataService = inject(ClientDataService);
  private readonly router = inject(Router);

  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected createClient(request: ClientRequest): void {
    this.saving.set(true);
    this.errorMessage.set(null);

    this.clientDataService.createClient(request).subscribe({
      next: () => {
        void this.clientContext.reload().finally(() => void this.router.navigate(['/clients']));
      },
      error: (error: { error?: string }) => {
        this.errorMessage.set(error.error ?? 'Unable to create the client.');
        this.saving.set(false);
      }
    });
  }

  protected navigateBack(): void {
    void this.router.navigate(['/clients']);
  }
}

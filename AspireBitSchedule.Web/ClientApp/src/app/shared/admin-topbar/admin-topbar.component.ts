import { ChangeDetectionStrategy, Component, HostListener, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-admin-topbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './admin-topbar.component.html',
  styleUrls: ['./admin-topbar.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminTopbarComponent {
  protected readonly administrationOpen = signal(false);

  protected toggleAdministrationMenu(): void {
    this.administrationOpen.update((open) => !open);
  }

  protected closeAdministrationMenu(): void {
    this.administrationOpen.set(false);
  }

  @HostListener('document:click')
  protected handleDocumentClick(): void {
    this.closeAdministrationMenu();
  }
}

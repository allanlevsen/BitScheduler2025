import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { ClientContextService } from './core/client-context/client-context.service';
import { AdminTopbarComponent } from './shared/admin-topbar/admin-topbar.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, AdminTopbarComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {
  protected readonly clientContext = inject(ClientContextService);

  public constructor() {
    void this.clientContext.initialize();
  }
}

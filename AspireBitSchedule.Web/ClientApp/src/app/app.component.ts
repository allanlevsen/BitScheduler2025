import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AdminTopbarComponent } from './shared/admin-topbar/admin-topbar.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, AdminTopbarComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {}

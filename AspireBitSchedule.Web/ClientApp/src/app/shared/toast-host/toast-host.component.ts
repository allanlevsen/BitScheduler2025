import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { ToastMessage, ToastService } from '../../core/toast/toast.service';

@Component({
  selector: 'app-toast-host',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast-host.component.html',
  styleUrls: ['./toast-host.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ToastHostComponent {
  protected readonly toastService = inject(ToastService);

  protected dismiss(id: number): void {
    this.toastService.dismiss(id);
  }

  protected trackToast(_index: number, toast: ToastMessage): number {
    return toast.id;
  }
}

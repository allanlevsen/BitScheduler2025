import { Injectable, signal } from '@angular/core';

export type ToastVariant = 'success' | 'error' | 'info' | 'warning';

export interface ToastMessage {
  id: number;
  message: string;
  title: string | null;
  variant: ToastVariant;
  timeoutMs: number;
}

export interface ShowToastOptions {
  title?: string | null;
  timeoutMs?: number;
  variant?: ToastVariant;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  public readonly toasts = signal<ToastMessage[]>([]);

  private nextId = 1;

  public show(message: string, options?: ShowToastOptions): void {
    const normalizedMessage = message.trim();
    if (!normalizedMessage) {
      return;
    }

    const toast: ToastMessage = {
      id: this.nextId++,
      message: normalizedMessage,
      title: options?.title?.trim() || null,
      variant: options?.variant ?? 'info',
      timeoutMs: options?.timeoutMs ?? 6000
    };

    this.toasts.update((currentToasts) => [...currentToasts, toast]);

    if (toast.timeoutMs > 0) {
      window.setTimeout(() => {
        this.dismiss(toast.id);
      }, toast.timeoutMs);
    }
  }

  public success(message: string, title?: string | null): void {
    this.show(message, { title, variant: 'success' });
  }

  public error(message: string, title?: string | null): void {
    this.show(message, { title, variant: 'error', timeoutMs: 8000 });
  }

  public info(message: string, title?: string | null): void {
    this.show(message, { title, variant: 'info' });
  }

  public warning(message: string, title?: string | null): void {
    this.show(message, { title, variant: 'warning', timeoutMs: 7000 });
  }

  public dismiss(id: number): void {
    this.toasts.update((currentToasts) => currentToasts.filter((toast) => toast.id !== id));
  }
}

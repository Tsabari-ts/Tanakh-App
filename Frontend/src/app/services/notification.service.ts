import { Injectable, signal } from '@angular/core';

export interface AppNotification {
  text: string;
  type: 'error' | 'info';
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly _current = signal<AppNotification | null>(null);
  readonly current = this._current.asReadonly();
  private dismissTimeout: any;

  showError(text: string): void {
    this.show({ text, type: 'error' });
  }

  showInfo(text: string): void {
    this.show({ text, type: 'info' });
  }

  private show(notification: AppNotification): void {
    clearTimeout(this.dismissTimeout);
    this._current.set(notification);
    this.dismissTimeout = setTimeout(() => this._current.set(null), 6000);
  }

  dismiss(): void {
    clearTimeout(this.dismissTimeout);
    this._current.set(null);
  }
}

import { Injectable, signal } from '@angular/core';

export type ErrorState =
  | { kind: 'none' }
  | { kind: 'fatal' }
  | { kind: 'reload'; message: string };

@Injectable({ providedIn: 'root' })
export class ErrorStateService {
  private readonly _state = signal<ErrorState>({ kind: 'none' });
  readonly state = this._state.asReadonly();

  showFatal(): void {
    this._state.set({ kind: 'fatal' });
  }

  showReloadPrompt(message: string): void {
    this._state.set({ kind: 'reload', message });
  }

  clear(): void {
    this._state.set({ kind: 'none' });
  }
}

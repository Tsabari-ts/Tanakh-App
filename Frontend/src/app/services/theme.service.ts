import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'tanach.theme';
type Theme = 'light' | 'dark';

/**
 * Manual dark/light override, layered on top of the existing
 * `prefers-color-scheme` system default (see tokens.dark-palette in
 * _tokens.scss and its two call sites in _a11y-modes.scss). No stored
 * preference means "follow the system" - this only ever writes an explicit
 * choice once the user actually toggles it.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly systemPrefersDark = window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
  private readonly _explicitTheme = signal<Theme | null>(this.loadStoredTheme());
  readonly isDark = signal(this.computeIsDark());

  constructor() {
    this.applyToDom();
  }

  toggle(): void {
    const next: Theme = this.isDark() ? 'light' : 'dark';
    this._explicitTheme.set(next);
    localStorage.setItem(STORAGE_KEY, next);
    this.isDark.set(next === 'dark');
    this.applyToDom();
  }

  private computeIsDark(): boolean {
    const stored = this._explicitTheme();
    return stored ? stored === 'dark' : this.systemPrefersDark;
  }

  private applyToDom(): void {
    const stored = this._explicitTheme();
    if (stored) {
      document.documentElement.setAttribute('data-theme', stored);
    } else {
      document.documentElement.removeAttribute('data-theme');
    }
  }

  private loadStoredTheme(): Theme | null {
    const value = localStorage.getItem(STORAGE_KEY);
    return value === 'dark' || value === 'light' ? value : null;
  }
}

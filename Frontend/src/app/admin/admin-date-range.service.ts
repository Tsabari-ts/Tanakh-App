import { Injectable, computed, signal } from '@angular/core';
import { DateRange, DateRangePreset } from './admin.models';
import { resolveDateRange } from './date-range.util';

const AUTO_REFRESH_INTERVAL_MS = 60_000;

@Injectable({
  providedIn: 'root'
})
export class AdminDateRangeService {
  private readonly presetSignal = signal<DateRangePreset>('7d');

  // Bumped by setPreset() and refresh() - modules re-fetch whenever this
  // changes, which covers both "range changed" and "user/auto refresh"
  // with a single signal to watch.
  private readonly refreshTokenSignal = signal(0);

  readonly preset = this.presetSignal.asReadonly();
  readonly refreshToken = this.refreshTokenSignal.asReadonly();
  readonly range = computed<DateRange>(() => resolveDateRange(this.presetSignal()));

  constructor() {
    setInterval(() => this.refresh(), AUTO_REFRESH_INTERVAL_MS);
  }

  setPreset(preset: DateRangePreset): void {
    this.presetSignal.set(preset);
    this.refresh();
  }

  refresh(): void {
    this.refreshTokenSignal.update(v => v + 1);
  }
}

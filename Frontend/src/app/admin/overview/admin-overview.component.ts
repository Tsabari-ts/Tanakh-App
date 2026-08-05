import { Component, ChangeDetectionStrategy, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AdminStatsService } from '../admin-stats.service';
import { AdminDateRangeService } from '../admin-date-range.service';
import { AdminOverview } from '../admin.models';

@Component({
  selector: 'app-admin-overview',
  templateUrl: './admin-overview.component.html',
  styleUrl: './admin-overview.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: []
})
export class AdminOverviewComponent {
  private readonly statsService = inject(AdminStatsService);
  private readonly dateRange = inject(AdminDateRangeService);
  private readonly destroyRef = inject(DestroyRef);

  readonly overview = signal<AdminOverview | null>(null);
  readonly loading = signal(false);
  readonly error = signal(false);

  constructor() {
    effect(() => {
      const range = this.dateRange.range();
      this.dateRange.refreshToken();
      this.load(range.from, range.to);
    });
  }

  private load(from: string, to: string): void {
    this.loading.set(true);
    this.error.set(false);

    this.statsService.getOverview({ from, to })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: overview => {
          this.overview.set(overview);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(true);
          this.loading.set(false);
        }
      });
  }

  formatChange(changePercent: number | null): string {
    if (changePercent === null) {
      return 'חדש';
    }
    const sign = changePercent > 0 ? '+' : '';
    return `${sign}${changePercent}%`;
  }
}

import { Component, ChangeDetectionStrategy, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AdminAuthService } from '../admin-auth.service';
import { AdminDateRangeService } from '../admin-date-range.service';
import { DateRangePreset } from '../admin.models';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-admin-shell',
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, RouterOutlet]
})
export class AdminShellComponent {
  private readonly authService = inject(AdminAuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  readonly dateRange = inject(AdminDateRangeService);
  readonly theme = inject(ThemeService);

  readonly presets: { id: DateRangePreset; label: string }[] = [
    { id: 'today', label: 'היום' },
    { id: '7d', label: '7 ימים' },
    { id: '30d', label: '30 ימים' }
  ];

  setPreset(preset: DateRangePreset): void {
    this.dateRange.setPreset(preset);
  }

  refresh(): void {
    this.dateRange.refresh();
  }

  logout(): void {
    this.authService.logout()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.router.navigate(['/', environment.adminRoutePath, 'login']));
  }
}

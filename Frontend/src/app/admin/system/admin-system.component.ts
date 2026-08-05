import { DatePipe } from '@angular/common';
import { Component, ChangeDetectionStrategy, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { AdminSystemService } from '../admin-system.service';
import { FeatureFlag, SystemHealth } from '../admin.models';

@Component({
  selector: 'app-admin-system',
  templateUrl: './admin-system.component.html',
  styleUrl: './admin-system.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, FormsModule]
})
export class AdminSystemComponent {
  private readonly systemService = inject(AdminSystemService);
  private readonly destroyRef = inject(DestroyRef);

  readonly health = signal<SystemHealth | null>(null);
  readonly flags = signal<FeatureFlag[]>([]);

  maintenanceEnabled = false;
  maintenanceMessage = '';
  readonly maintenanceSaved = signal(false);

  bannerText = '';
  bannerExpiresAt = '';
  readonly bannerSaved = signal(false);

  newFlagName = '';

  constructor() {
    this.loadHealth();
    this.loadMaintenance();
    this.loadBanner();
    this.loadFlags();
  }

  saveMaintenance(): void {
    this.systemService.setMaintenance(this.maintenanceEnabled, this.maintenanceMessage || null)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.maintenanceSaved.set(true);
        setTimeout(() => this.maintenanceSaved.set(false), 2000);
      });
  }

  saveBanner(): void {
    if (!this.bannerText || !this.bannerExpiresAt) {
      return;
    }

    this.systemService.setBanner(this.bannerText, new Date(this.bannerExpiresAt).toISOString())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.bannerSaved.set(true);
        setTimeout(() => this.bannerSaved.set(false), 2000);
      });
  }

  clearBanner(): void {
    this.systemService.clearBanner()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.bannerText = '';
        this.bannerExpiresAt = '';
      });
  }

  addFlag(): void {
    const name = this.newFlagName.trim();
    if (!name) {
      return;
    }

    this.systemService.setFlag(name, false)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.newFlagName = '';
        this.loadFlags();
      });
  }

  toggleFlag(flag: FeatureFlag): void {
    this.systemService.setFlag(flag.name, !flag.enabled)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadFlags());
  }

  deleteFlag(flag: FeatureFlag): void {
    this.systemService.deleteFlag(flag.name)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadFlags());
  }

  formatBytes(bytes: number | null): string {
    if (bytes === null) {
      return '—';
    }
    const gb = bytes / (1024 * 1024 * 1024);
    return `${gb.toFixed(1)} GB`;
  }

  formatUptime(seconds: number): string {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    return `${hours} שעות, ${minutes} דקות`;
  }

  private loadHealth(): void {
    this.systemService.getHealth()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(health => this.health.set(health));
  }

  private loadMaintenance(): void {
    this.systemService.getMaintenance()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(status => {
        this.maintenanceEnabled = status.enabled;
        this.maintenanceMessage = status.message ?? '';
      });
  }

  private loadBanner(): void {
    this.systemService.getBanner()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(banner => {
        if (banner) {
          this.bannerText = banner.text;
          this.bannerExpiresAt = banner.expiresAt.slice(0, 16);
        }
      });
  }

  private loadFlags(): void {
    this.systemService.getFlags()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(flags => this.flags.set(flags));
  }
}

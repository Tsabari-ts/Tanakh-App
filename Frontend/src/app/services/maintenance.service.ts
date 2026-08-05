import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiCallService } from './api-call.service';

export interface MaintenanceStatus {
  enabled: boolean;
  message: string | null;
}

/**
 * Checked once at boot (see provideAppInitializer in app.config.ts). The
 * admin panel bypasses this entirely - it needs to stay reachable so an
 * admin can turn maintenance mode back off, so the check is skipped
 * whenever the current URL is under the hidden admin route.
 */
@Injectable({ providedIn: 'root' })
export class MaintenanceService {
  private readonly apiCall = inject(ApiCallService);
  readonly status = signal<MaintenanceStatus | null>(null);

  async check(): Promise<void> {
    if (window.location.pathname.startsWith(`/${environment.adminRoutePath}`)) {
      return;
    }

    try {
      const status = await firstValueFrom(this.apiCall.getMaintenanceStatus());
      this.status.set(status);
    } catch {
      // Fail open - a broken status check must never itself take the app down.
      this.status.set({ enabled: false, message: null });
    }
  }
}

import { Injectable, inject, signal } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AppUpdateService {
  // Optional: SwUpdate is only provided when provideServiceWorker() is in the
  // injector tree, which most component-level tests don't include. Without
  // {optional: true} here, merely injecting AppComponent (which many other
  // components do) would throw in any test that doesn't set up the full app config.
  private readonly swUpdate = inject(SwUpdate, { optional: true });
  readonly updateAvailable = signal(false);

  init(): void {
    if (!this.swUpdate?.isEnabled) return;

    this.swUpdate.versionUpdates
      .pipe(filter((e): e is VersionReadyEvent => e.type === 'VERSION_READY'))
      .subscribe(() => this.updateAvailable.set(true));

    this.swUpdate.unrecoverable.subscribe(() => document.location.reload());

    // check for updates every 6 hours
    setInterval(() => void this.swUpdate!.checkForUpdate(), 6 * 60 * 60 * 1000);
  }

  applyUpdate(): void {
    void this.swUpdate?.activateUpdate().then(() => document.location.reload());
  }
}

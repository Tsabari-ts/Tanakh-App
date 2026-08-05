import { Component, ChangeDetectionStrategy, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApiCallService } from '../../services/api-call.service';

const DISMISSED_KEY_PREFIX = 'tanach.announcementDismissed.';

/**
 * Admin-published banner (Frontend/src/app/admin/system) - structurally
 * mirrors CookieBannerComponent but reads its content from the backend
 * instead of a static string. Dismissal is keyed by the banner text itself,
 * so publishing a *new* banner re-appears even if a prior one was dismissed.
 */
@Component({
  selector: 'app-announcement-banner',
  templateUrl: './announcement-banner.component.html',
  styleUrl: './announcement-banner.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AnnouncementBannerComponent {
  private readonly apiCall = inject(ApiCallService);
  private readonly destroyRef = inject(DestroyRef);

  readonly text = signal<string | null>(null);
  readonly dismissed = signal(false);

  constructor() {
    this.apiCall.getAnnouncementBanner()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: banner => {
          if (!banner) {
            return;
          }
          this.text.set(banner.text);
          this.dismissed.set(localStorage.getItem(this.dismissKey(banner.text)) === 'true');
        },
        error: () => { /* fail silent - a broken banner check must not disrupt the app */ }
      });
  }

  dismiss(): void {
    const text = this.text();
    if (text) {
      localStorage.setItem(this.dismissKey(text), 'true');
    }
    this.dismissed.set(true);
  }

  private dismissKey(text: string): string {
    return `${DISMISSED_KEY_PREFIX}${text}`;
  }
}

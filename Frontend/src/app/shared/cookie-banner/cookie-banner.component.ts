import { Component, ChangeDetectionStrategy, signal } from '@angular/core';

const DISMISSED_KEY = 'tanach.cookieBannerDismissed';

@Component({
  selector: 'app-cookie-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!dismissed()) {
      <div class="cookie-banner card elev-md" role="status">
        <p i18n="@@cookieBanner.text">
          אתר זה משתמש בעוגיות (cookies) הנחוצות לתפעולו התקין, כגון שמירת הגדרות תצוגה ונגישות במכשיר שלך.
        </p>
        <button type="button" class="btn btn-primary" (click)="dismiss()" i18n="@@cookieBanner.dismiss">הבנתי</button>
      </div>
    }
  `,
  styles: [`
    .cookie-banner {
      position: fixed;
      z-index: 800;
      inset-inline: var(--space-4);
      bottom: calc(var(--space-4) + env(safe-area-inset-bottom, 0px));
      max-width: 480px;
      margin-inline: auto;
      flex-direction: row;
      align-items: center;
      gap: var(--space-4);
    }
    .cookie-banner p {
      margin: 0;
      font-size: var(--font-size-sm);
      color: var(--color-text);
      flex: 1;
    }
    .cookie-banner .btn {
      flex: none;
    }
  `],
})
export class CookieBannerComponent {
  readonly dismissed = signal(localStorage.getItem(DISMISSED_KEY) === 'true');

  dismiss(): void {
    localStorage.setItem(DISMISSED_KEY, 'true');
    this.dismissed.set(true);
  }
}

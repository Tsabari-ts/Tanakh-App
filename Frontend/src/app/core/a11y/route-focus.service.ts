import { Injectable, inject, DestroyRef } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { filter } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

/**
 * On every route change: move focus to the new page's h1 (falling back to
 * <main>) and announce it, so keyboard/screen-reader users get the same
 * "new page" signal sighted users get from the visual change. Angular's
 * router already updates document.title from each route's `title` (see
 * app.routes.ts / DefaultTitleStrategy) - this only handles focus + the
 * live-region announcement, which the router does not do on its own.
 */
@Injectable({ providedIn: 'root' })
export class RouteFocusService {
  private router = inject(Router);
  private live = inject(LiveAnnouncer);
  private destroyRef = inject(DestroyRef);
  private isFirstNavigation = true;

  init(): void {
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(() => {
      // Skip the very first navigation (app boot) - there is nothing to
      // announce "arriving at" yet, and moving focus away from the URL
      // bar on load would be surprising rather than helpful.
      if (this.isFirstNavigation) {
        this.isFirstNavigation = false;
        return;
      }

      setTimeout(() => this.focusAndAnnounce());
    });
  }

  private focusAndAnnounce(): void {
    const target =
      document.querySelector<HTMLElement>('main h1') ??
      document.getElementById('main-content');
    if (!target) {
      return;
    }

    if (!target.hasAttribute('tabindex')) {
      target.setAttribute('tabindex', '-1');
    }
    target.focus({ preventScroll: true });
    window.scrollTo({ top: 0, behavior: 'auto' });

    const label = target.textContent?.trim() || document.title;
    if (label) {
      this.live.announce(label, 'polite');
    }
  }
}

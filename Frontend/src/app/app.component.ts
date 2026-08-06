import { Component, ChangeDetectionStrategy, inject, signal, afterNextRender } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Location } from '@angular/common';
import { Dir } from '@angular/cdk/bidi';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { environment } from '../environments/environment';
import { NotificationService } from './services/notification.service';
import { ErrorStateService } from './services/error-state.service';
import { ErrorScreenComponent } from './shared/error-screen/error-screen.component';
import { AppUpdateService } from './core/app-update.service';
import { SkipLinkComponent } from './shared/a11y/skip-link/skip-link.component';
import { RouteFocusService } from './core/a11y/route-focus.service';
import { AccessibilityWidgetComponent } from './shared/a11y/accessibility-widget/accessibility-widget.component';
import { LegalDialogService } from './shared/legal/legal-dialog.service';
import { LegalDocId } from './shared/legal/legal-content';
import { CookieBannerComponent } from './shared/cookie-banner/cookie-banner.component';
import { MaintenanceScreenComponent } from './shared/maintenance-screen/maintenance-screen.component';
import { AnnouncementBannerComponent } from './shared/announcement-banner/announcement-banner.component';
import { MaintenanceService } from './services/maintenance.service';
import { WHATSAPP_CONTACT_URL } from './shared/contact-links';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Dir, RouterOutlet, ErrorScreenComponent, SkipLinkComponent, AccessibilityWidgetComponent, CookieBannerComponent, MaintenanceScreenComponent, AnnouncementBannerComponent]
})
export class AppComponent {
  title = 'Tanakh';
  readonly showButton = signal(false);
  /** Where the header's back button navigates to - set by each routed
   *  component's constructor to its actual parent screen, since browser
   *  history (Location.back()) is unreliable (deep links, refreshes). */
  readonly backTarget = signal<any[] | null>(null);
  readonly whatsappContactUrl = WHATSAPP_CONTACT_URL;
  // The hidden admin panel is a separate internal tool, not a page of the
  // public reader-facing site - it needs its own header/nav (admin-shell)
  // instead of the public site's header/footer/cookie-banner/a11y-widget/
  // background image sitting underneath (and, for the header, visually on
  // top of) it.
  readonly isAdminRoute = toSignal(
    inject(Router).events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(event => event.urlAfterRedirects.startsWith(`/${environment.adminRoutePath}`))),
    { initialValue: window.location.pathname.startsWith(`/${environment.adminRoutePath}`) });
  readonly notifications = inject(NotificationService);
  readonly errorState = inject(ErrorStateService);
  readonly appUpdate = inject(AppUpdateService);
  readonly maintenance = inject(MaintenanceService);
  private readonly legal = inject(LegalDialogService);

  constructor(private location: Location, private router: Router, routeFocus: RouteFocusService) {
    routeFocus.init();

    // Lets the accessibility statement / terms of use / privacy policy be
    // linked to directly (e.g. for a reviewer or authority) even though
    // they only open as a dialog and have no dedicated route - see ADR
    // discussion in the a11y implementation doc.
    afterNextRender(() => {
      const params = new URLSearchParams(window.location.search);
      if (params.get('a11y') === 'statement') {
        this.legal.open('accessibility');
      }
      const legalDoc = params.get('legal');
      if (legalDoc === 'terms' || legalDoc === 'privacy') {
        this.legal.open(legalDoc);
      }
    });
  }

  goBack(): void {
    const target = this.backTarget();
    if (target) {
      this.router.navigate(target);
    } else {
      this.location.back();
    }
  }

  goSettings(): void {
    this.router.navigate(['/settings']);
  }

  openLegal(id: LegalDocId): void {
    this.legal.open(id);
  }
}

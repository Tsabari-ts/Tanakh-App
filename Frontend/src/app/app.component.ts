import { Component, ChangeDetectionStrategy, inject, signal, afterNextRender } from '@angular/core';
import { Location, NgClass } from '@angular/common';
import { Dir } from '@angular/cdk/bidi';
import { RouterOutlet } from '@angular/router';
import { NotificationService } from './services/notification.service';
import { ErrorStateService } from './services/error-state.service';
import { ErrorScreenComponent } from './shared/error-screen/error-screen.component';
import { AppUpdateService } from './core/app-update.service';
import { SkipLinkComponent } from './shared/a11y/skip-link/skip-link.component';
import { RouteFocusService } from './core/a11y/route-focus.service';
import { AccessibilityWidgetComponent } from './shared/a11y/accessibility-widget/accessibility-widget.component';
import { AccessibilityStatementService } from './shared/a11y/accessibility-statement-dialog/accessibility-statement.service';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrl: './app.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Dir, NgClass, RouterOutlet, ErrorScreenComponent, SkipLinkComponent, AccessibilityWidgetComponent]
})
export class AppComponent {
  title = 'Tanakh';
  readonly showButton = signal(false);
  returnIcon:string = 'return-icon';
  readonly notifications = inject(NotificationService);
  readonly errorState = inject(ErrorStateService);
  readonly appUpdate = inject(AppUpdateService);
  private readonly statement = inject(AccessibilityStatementService);

  constructor(private location: Location, routeFocus: RouteFocusService) {
    routeFocus.init();

    // Lets the accessibility statement be linked to directly (e.g. for a
    // reviewer or authority) even though it only opens as a dialog and has
    // no dedicated route - see ADR discussion in the a11y implementation doc.
    afterNextRender(() => {
      if (new URLSearchParams(window.location.search).get('a11y') === 'statement') {
        this.statement.open();
      }
    });
  }

  goBack(): void {
    this.location.back();
  }

  openStatement(): void {
    this.statement.open();
  }
}

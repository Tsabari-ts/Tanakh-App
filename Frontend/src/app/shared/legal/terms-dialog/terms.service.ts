import { Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { TermsDialogComponent } from './terms-dialog.component';

/**
 * Single entry point for opening the terms-of-use dialog, used by the
 * footer link, the subscribe (reminder registration) consent checkbox, and
 * the `?legal=terms` deep link (app.component.ts) so all three stay in
 * sync with one MatDialog config - mirrors AccessibilityStatementService.
 */
@Injectable({ providedIn: 'root' })
export class TermsService {
  constructor(private dialog: MatDialog) {}

  open(): void {
    if (this.dialog.openDialogs.some(d => d.componentInstance instanceof TermsDialogComponent)) {
      return;
    }
    this.dialog.open(TermsDialogComponent, {
      width: '700px',
      maxWidth: '95vw',
      autoFocus: 'dialog',
    });
  }
}

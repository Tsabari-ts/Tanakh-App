import { Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { AccessibilityStatementDialogComponent } from './accessibility-statement-dialog.component';

/**
 * Single entry point for opening the accessibility statement dialog, used by
 * the footer link, the widget panel, and the `?a11y=statement` deep link
 * (app.component.ts) so all three stay in sync with one MatDialog config.
 */
@Injectable({ providedIn: 'root' })
export class AccessibilityStatementService {
  constructor(private dialog: MatDialog) {}

  open(): void {
    if (this.dialog.openDialogs.some(d => d.componentInstance instanceof AccessibilityStatementDialogComponent)) {
      return;
    }
    this.dialog.open(AccessibilityStatementDialogComponent, {
      width: '700px',
      maxWidth: '95vw',
      autoFocus: 'dialog',
    });
  }
}

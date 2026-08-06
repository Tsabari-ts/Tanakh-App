import { Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { LEGAL_DOCS_META, LegalDocId } from './legal-content';

/**
 * Single entry point for opening any of the three legal dialogs (terms of
 * use, privacy policy, accessibility statement) - used by the footer links,
 * the subscribe (reminder registration) consent checkbox, the accessibility
 * widget panel, and the `?legal=`/`?a11y=statement` deep links
 * (app.component.ts) so all call sites stay in sync with one MatDialog
 * config. Replaces the former TermsService/AccessibilityStatementService.
 *
 * The component and the (large) HTML bodies are dynamically imported here
 * rather than statically, so they land in a lazy chunk instead of bloating
 * the eagerly-loaded main bundle - see legal-content-html.ts.
 */
@Injectable({ providedIn: 'root' })
export class LegalDialogService {
  constructor(private dialog: MatDialog) {}

  async open(id: LegalDocId): Promise<void> {
    const [{ LegalModalComponent }, { LEGAL_HTML }] = await Promise.all([
      import('./legal-modal/legal-modal.component'),
      import('./legal-content-html'),
    ]);

    if (this.dialog.openDialogs.some(d =>
      d.componentInstance instanceof LegalModalComponent && d.componentInstance.data.id === id)) {
      return;
    }

    this.dialog.open(LegalModalComponent, {
      data: { ...LEGAL_DOCS_META[id], html: LEGAL_HTML[id] },
      width: '700px',
      maxWidth: '95vw',
      autoFocus: 'dialog',
    });
  }
}

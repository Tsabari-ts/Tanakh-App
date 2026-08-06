import { Component, ChangeDetectionStrategy, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions } from '@angular/material/dialog';
import { CdkScrollable } from '@angular/cdk/scrolling';
import { A11yModule } from '@angular/cdk/a11y';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { LegalDoc } from '../legal-content';

@Component({
  selector: 'app-legal-modal',
  standalone: true,
  imports: [MatDialogTitle, MatDialogContent, MatDialogActions, CdkScrollable, A11yModule],
  templateUrl: './legal-modal.component.html',
  styleUrl: './legal-modal.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LegalModalComponent {
  // Content is developer-authored only (legal-content.ts), never user/API
  // input - see the security note in legal-content.ts.
  readonly safeHtml: SafeHtml;

  constructor(
    public dialogRef: MatDialogRef<LegalModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: LegalDoc,
    sanitizer: DomSanitizer,
  ) {
    this.safeHtml = sanitizer.bypassSecurityTrustHtml(data.html);
  }

  close(): void {
    this.dialogRef.close();
  }

  print(): void {
    window.print();
  }
}

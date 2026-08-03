import { Component, ChangeDetectionStrategy } from '@angular/core';
import { MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions } from '@angular/material/dialog';
import { CdkScrollable } from '@angular/cdk/scrolling';
import { A11yModule } from '@angular/cdk/a11y';

@Component({
  selector: 'app-terms-dialog',
  standalone: true,
  imports: [MatDialogTitle, MatDialogContent, MatDialogActions, CdkScrollable, A11yModule],
  templateUrl: './terms-dialog.component.html',
  styleUrl: './terms-dialog.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TermsDialogComponent {
  constructor(public dialogRef: MatDialogRef<TermsDialogComponent>) {}

  close(): void {
    this.dialogRef.close();
  }

  print(): void {
    window.print();
  }
}

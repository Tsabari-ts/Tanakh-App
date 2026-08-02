import { Component, ChangeDetectionStrategy } from '@angular/core';
import { MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions } from '@angular/material/dialog';
import { CdkScrollable } from '@angular/cdk/scrolling';
import { A11yModule } from '@angular/cdk/a11y';

@Component({
  selector: 'app-accessibility-statement-dialog',
  standalone: true,
  imports: [MatDialogTitle, MatDialogContent, MatDialogActions, CdkScrollable, A11yModule],
  templateUrl: './accessibility-statement-dialog.component.html',
  styleUrl: './accessibility-statement-dialog.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessibilityStatementDialogComponent {
  constructor(public dialogRef: MatDialogRef<AccessibilityStatementDialogComponent>) {}

  close(): void {
    this.dialogRef.close();
  }

  print(): void {
    window.print();
  }
}

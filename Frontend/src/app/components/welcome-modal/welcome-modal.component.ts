import { Component, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { CdkScrollable } from '@angular/cdk/scrolling';

@Component({
    selector: 'app-welcome-modal',
    templateUrl: './welcome-modal.component.html',
    styleUrl: './welcome-modal.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatDialogTitle, MatIcon, CdkScrollable, MatDialogContent, MatDialogActions]
})
export class WelcomeModalComponent {
  constructor(public dialogRef: MatDialogRef<WelcomeModalComponent>,
              private router: Router) {}

  closeDialog() {
    this.dialogRef.close();
  }

  closeAndGoSetting(){
    this.dialogRef.close();
    this.router.navigate([`/settings`]); 
  }
}

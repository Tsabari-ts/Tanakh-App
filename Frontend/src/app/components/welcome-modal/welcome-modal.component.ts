import { Component, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { CdkScrollable } from '@angular/cdk/scrolling';

@Component({
    selector: 'app-welcome-modal',
    templateUrl: './welcome-modal.component.html',
    styleUrl: './welcome-modal.component.css',
    changeDetection: ChangeDetectionStrategy.Eager, // TODO(F-03): remove after signals migration
    imports: [MatDialogTitle, MatIcon, CdkScrollable, MatDialogContent, MatDialogActions]
})
export class WelcomeModalComponent {
  constructor(public dialogRef: MatDialogRef<WelcomeModalComponent>,
              private router: Router) {}

  data:any = {
    title: 'ברוכים הבאים',
    content: 'תוכלו לקרוא פרק אחד בכל יום ולסיים את כל התנ"ך.  על ידי לחיצה על הכפתור למטה תוכלו לעבור לעמוד ההגדרות ולהגדיר האם ברצונכם להירשם ולקבל תזכורת יומית בהודעת אמממסמ ישירות לפלאפון שלכם כמו כן תוכלו להוריד את האפליקציה אליכם לפלאפון (שימו לב כי האפליקציה כמעט לא משתמשת בנפח אחסון שלכם).',
  };

  closeDialog() {
    this.dialogRef.close();
  }

  closeAndGoSetting(){
    this.dialogRef.close();
    this.router.navigate([`/settings`]); 
  }
}

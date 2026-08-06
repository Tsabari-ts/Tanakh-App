import { Injectable } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { WelcomeModalComponent } from '../components/welcome-modal/welcome-modal.component';
import { ReadPermissionComponent } from '../components/read-permission/read-permission.component';

@Injectable({
  providedIn: 'root'
})

export class DialogService {
  constructor(private dialog: MatDialog) {}
  private dialogShownKey = 'userHasSeenWelcomeModal';
  private readonly welcomeModalCooldownMs = 1000 * 60 * 60 * 24 * 30 * 6; // ~6 months

  openWelcomeDialog(): void {
    const dialogRef = this.dialog.open(WelcomeModalComponent, {
      width: '500px',
      disableClose: true
    });

    dialogRef.afterClosed().subscribe(() => {
      this.markDialogAsShown();
    });
  }

  shouldShowWelcomeDialog(): boolean {
    const lastShown = Number(localStorage.getItem(this.dialogShownKey));
    return !lastShown || Date.now() - lastShown > this.welcomeModalCooldownMs;
  }

  markDialogAsShown(): void {
    localStorage.setItem(this.dialogShownKey, Date.now().toString());
  }

  openReadPermissionDialog(dialogData:any): MatDialogRef<ReadPermissionComponent> {
    return this.dialog.open(ReadPermissionComponent, {
      data: dialogData, 
      width: '500px',
      disableClose: true,
    });
  }
}

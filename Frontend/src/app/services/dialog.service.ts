import { Injectable } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { WelcomeModalComponent } from '../components/welcome-modal/welcome-modal.component';
import { SubscribeComponent } from '../components/subscribe/subscribe.component';
import { ReadPermissionComponent } from '../components/read-permission/read-permission.component';

@Injectable({
  providedIn: 'root'
})

export class DialogService {
  constructor(private dialog: MatDialog) {}
  private dialogShownKey = 'userHasSeenWelcomeModal';

  openWelcomeDialog(): void {
    const dialogRef = this.dialog.open(WelcomeModalComponent, {
      width: '500px',
      disableClose: true
    });
  
    dialogRef.afterClosed().subscribe(() => {
      this.markDialogAsShown();
    });
  }

  markDialogAsShown(): void {
    localStorage.setItem(this.dialogShownKey, 'true');
  }

  openReadPermissionDialog(dialogData:any): MatDialogRef<ReadPermissionComponent> {
    return this.dialog.open(ReadPermissionComponent, {
      data: dialogData, 
      width: '500px',
      disableClose: true,
    });
  }

  openSubscribeDialog(): MatDialogRef<SubscribeComponent> {
    return this.dialog.open(SubscribeComponent, {
      width: '500px',
      disableClose: true,
    });
  }
}

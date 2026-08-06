import { Injectable } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { WelcomeModalComponent } from '../components/welcome-modal/welcome-modal.component';
import { ReadPermissionComponent } from '../components/read-permission/read-permission.component';
import { environment } from '../../environments/environment';

const WELCOME_MODAL_DELAY_MS = 10000;

@Injectable({
  providedIn: 'root'
})

export class DialogService {
  constructor(private dialog: MatDialog) {}
  private dialogShownKey = 'userHasSeenWelcomeModal';
  private readonly welcomeModalCooldownMs = 1000 * 60 * 60 * 24 * 30 * 6; // ~6 months

  // Runs once at app bootstrap (provideAppInitializer in app.config.ts) -
  // not tied to any routed component, so it fires 10s after the app loads
  // no matter what page the user is on or navigates to in between. The old
  // version lived in HomeComponent's ngOnInit/ngOnDestroy, so leaving the
  // home page before the timer fired cancelled it outright.
  initWelcomeDialog(): void {
    if (!this.shouldShowWelcomeDialog()) {
      return;
    }

    setTimeout(() => {
      const path = window.location.pathname;
      // Skip the entrance name-gate (a welcome popup on top of the site's
      // own "welcome" screen is redundant) and the hidden admin panel.
      const onEntrance = path === '/' || path.startsWith('/entrance');
      const onAdmin = path.startsWith(`/${environment.adminRoutePath}`);
      if (onEntrance || onAdmin) {
        return;
      }
      this.openWelcomeDialog();
    }, WELCOME_MODAL_DELAY_MS);
  }

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

import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class PwaInstallService {
  deferredPrompt: any;
  isPwaInstalled = localStorage.getItem('pwaInstalled') === 'true';

  constructor() { 
    if (typeof window !== 'undefined') {
      window.addEventListener('beforeinstallprompt', (event) => {
        // Prevent the mini-infobar from appearing on mobile
        event.preventDefault();
        // Stash the event so it can be triggered later.
        this.deferredPrompt = event;
      });
    }
  }

  installPWA() {
    if (this.deferredPrompt) {
      this.deferredPrompt.prompt();
      this.deferredPrompt.userChoice.then((choiceResult: { outcome: 'accepted' | 'dismissed' }) => {
        if (choiceResult.outcome === 'accepted') {
          localStorage.setItem('pwaInstalled', 'true');
          this.isPwaInstalled = true;
          console.log('User accepted the install prompt');
        } else {
          console.log('User dismissed the install prompt');
        }
        this.deferredPrompt = null;
      });
    }
  }

  checkServiceWorkerStatus() {
    navigator.serviceWorker.getRegistration().then((registration) => {
      if (!registration || !registration.active) {
        localStorage.removeItem('pwaInstalled');
        this.isPwaInstalled = false;
      }
    });
  }
  
}

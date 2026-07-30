import { ApplicationConfig } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withXhr, withInterceptorsFromDi } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideServiceWorker } from '@angular/service-worker';
import { WelcomeModalComponent } from './components/welcome-modal/welcome-modal.component';
import { SubscribeComponent } from './components/subscribe/subscribe.component';
import { ReadPermissionComponent } from './components/read-permission/read-permission.component';
import { routes } from './app.routes';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withComponentInputBinding()),
    provideAnimations(),
    provideHttpClient(withXhr(), withInterceptorsFromDi()),
    provideServiceWorker('ngsw-worker.js', {
      enabled: environment.enableServiceWorker,
      registrationStrategy: 'registerWhenStable:30000'
    }),
    WelcomeModalComponent,
    SubscribeComponent,
    ReadPermissionComponent
  ]
};

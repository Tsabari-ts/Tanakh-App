import { ApplicationConfig, ErrorHandler, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding, withPreloading, PreloadAllModules } from '@angular/router';
import { provideHttpClient, withXhr, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideServiceWorker } from '@angular/service-worker';
import { WelcomeModalComponent } from './components/welcome-modal/welcome-modal.component';
import { SubscribeComponent } from './components/subscribe/subscribe.component';
import { ReadPermissionComponent } from './components/read-permission/read-permission.component';
import { routes } from './app.routes';
import { environment } from '../environments/environment';
import { retryInterceptor } from './core/interceptors/retry.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { GlobalErrorHandler } from './core/global-error-handler';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideBrowserGlobalErrorListeners(),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    provideRouter(routes, withComponentInputBinding(), withPreloading(PreloadAllModules)),
    provideAnimations(),
    provideHttpClient(withXhr(), withInterceptors([retryInterceptor, errorInterceptor])),
    provideServiceWorker('ngsw-worker.js', {
      enabled: environment.enableServiceWorker,
      registrationStrategy: 'registerWhenStable:30000'
    }),
    WelcomeModalComponent,
    SubscribeComponent,
    ReadPermissionComponent
  ]
};

import { ApplicationConfig, ErrorHandler, inject, provideAppInitializer, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding, withPreloading, PreloadAllModules } from '@angular/router';
import { provideHttpClient, withXhr, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideServiceWorker } from '@angular/service-worker';
import { WelcomeModalComponent } from './components/welcome-modal/welcome-modal.component';
import { ReadPermissionComponent } from './components/read-permission/read-permission.component';
import { routes } from './app.routes';
import { environment } from '../environments/environment';
import { retryInterceptor } from './core/interceptors/retry.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { GlobalErrorHandler } from './core/global-error-handler';
import { AppUpdateService } from './core/app-update.service';
import { PwaInstallService } from './services/pwa-install.service';
import { TtsProvider } from './core/tts/tts-provider';
import { WebSpeechProvider } from './core/tts/web-speech-provider.service';
import { MaintenanceService } from './services/maintenance.service';
import { DialogService } from './services/dialog.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideBrowserGlobalErrorListeners(),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    // Swap to a CloudTtsProvider here in the future - see docs/adr for V-12.
    { provide: TtsProvider, useClass: WebSpeechProvider },
    provideRouter(routes, withComponentInputBinding(), withPreloading(PreloadAllModules)),
    provideAnimations(),
    provideHttpClient(withXhr(), withInterceptors([retryInterceptor, errorInterceptor])),
    provideServiceWorker('ngsw-worker.js', {
      enabled: environment.enableServiceWorker,
      registrationStrategy: 'registerWhenStable:30000'
    }),
    provideAppInitializer(() => inject(AppUpdateService).init()),
    provideAppInitializer(() => inject(PwaInstallService).init()),
    provideAppInitializer(() => inject(MaintenanceService).check()),
    provideAppInitializer(() => inject(DialogService).initWelcomeDialog()),
    WelcomeModalComponent,
    ReadPermissionComponent
  ]
};

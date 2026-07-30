import { ErrorHandler, Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';
import { ErrorStateService } from '../services/error-state.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly errorState = inject(ErrorStateService);

  handleError(error: unknown): void {
    if (!environment.production) console.error('[GlobalErrorHandler]', error);

    // chunk load failure (new deploy while the user has the app open) — a reload fixes it
    const message = (error as Error)?.message ?? String(error);
    if (/ChunkLoadError|Loading chunk .* failed|dynamically imported module/i.test(message)) {
      this.errorState.showReloadPrompt('גרסה חדשה זמינה. רענן את הדף כדי להמשיך.');
      return;
    }

    this.errorState.showFatal();
    this.report(error);
  }

  private report(error: unknown): void {
    if (!environment.production) return;
    // TODO(LAUNCH): wire to an error monitoring service (Sentry / App Insights / none).
    // Deliberately unwired — requires an account and a hosted environment.
    // See docs/LAUNCH-CHECKLIST.md, item L-04.
  }
}

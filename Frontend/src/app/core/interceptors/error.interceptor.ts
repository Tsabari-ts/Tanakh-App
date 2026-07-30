import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../../services/notification.service';

const MESSAGES: Record<number, string> = {
  0: $localize`:@@error.offline:אין חיבור לאינטרנט. בדוק את החיבור ונסה שוב.`,
  400: $localize`:@@error.badRequest:הבקשה אינה תקינה.`,
  401: $localize`:@@error.unauthorized:יש להתחבר מחדש.`,
  403: $localize`:@@error.forbidden:אין לך הרשאה לצפות בתוכן הזה.`,
  404: $localize`:@@error.notFound:התוכן המבוקש לא נמצא.`,
  429: $localize`:@@error.tooManyRequests:יותר מדי בקשות. נסה שוב בעוד רגע.`,
  500: $localize`:@@error.serverError:תקלה בשרת. אנחנו כבר על זה.`,
  503: $localize`:@@error.serviceUnavailable:השירות אינו זמין כרגע. נסה שוב בעוד מספר דקות.`,
};

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      console.error(`[HTTP ${err.status}] ${req.method} ${req.url}`, err);
      notifications.showError(MESSAGES[err.status] ?? $localize`:@@error.unexpected:אירעה שגיאה בלתי צפויה. נסה שוב.`);
      return throwError(() => err);
    }),
  );
};

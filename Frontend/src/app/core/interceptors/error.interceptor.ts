import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../../services/notification.service';

const MESSAGES: Record<number, string> = {
  0: 'אין חיבור לאינטרנט. בדוק את החיבור ונסה שוב.',
  400: 'הבקשה אינה תקינה.',
  401: 'יש להתחבר מחדש.',
  403: 'אין לך הרשאה לצפות בתוכן הזה.',
  404: 'התוכן המבוקש לא נמצא.',
  429: 'יותר מדי בקשות. נסה שוב בעוד רגע.',
  500: 'תקלה בשרת. אנחנו כבר על זה.',
  503: 'השירות אינו זמין כרגע. נסה שוב בעוד מספר דקות.',
};

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      console.error(`[HTTP ${err.status}] ${req.method} ${req.url}`, err);
      notifications.showError(MESSAGES[err.status] ?? 'אירעה שגיאה בלתי צפויה. נסה שוב.');
      return throwError(() => err);
    }),
  );
};

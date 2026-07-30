import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { errorInterceptor } from './error.interceptor';
import { NotificationService } from '../../services/notification.service';

describe('errorInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;
  let notifications: NotificationService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    notifications = TestBed.inject(NotificationService);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('shows a Hebrew message for a known status code', () => {
    httpClient.get('/api/books').subscribe({ error: () => {} });

    httpMock.expectOne('/api/books').flush('not found', { status: 404, statusText: 'Not Found' });

    expect(notifications.current()?.text).toBe('התוכן המבוקש לא נמצא.');
    expect(notifications.current()?.type).toBe('error');
  });

  it('shows a fallback message for an unmapped status code', () => {
    httpClient.get('/api/books').subscribe({ error: () => {} });

    httpMock.expectOne('/api/books').flush('teapot', { status: 418, statusText: "I'm a teapot" });

    expect(notifications.current()?.text).toBe('אירעה שגיאה בלתי צפויה. נסה שוב.');
  });

  it('re-throws the error so callers still see it', () => {
    let caught: unknown;
    httpClient.get('/api/books').subscribe({ error: (err) => (caught = err) });

    httpMock.expectOne('/api/books').flush('error', { status: 500, statusText: 'Server Error' });

    expect(caught).toBeTruthy();
  });
});

import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { retryInterceptor } from './retry.interceptor';

describe('retryInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([retryInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('retries a failing GET request up to 2 times on a 5xx before giving up', async () => {
    // Real timers, not fakeAsync/tick - those depend on zone.js's testing
    // bundle, which this project no longer loads (see F-04).
    const result = new Promise<number>((resolve) => {
      let errorCount = 0;
      httpClient.get('/api/books').subscribe({ error: () => resolve(++errorCount) });
    });

    httpMock.expectOne('/api/books').flush('error', { status: 500, statusText: 'Server Error' });
    await new Promise((r) => setTimeout(r, 1100));
    httpMock.expectOne('/api/books').flush('error', { status: 500, statusText: 'Server Error' });
    await new Promise((r) => setTimeout(r, 2100));
    httpMock.expectOne('/api/books').flush('error', { status: 500, statusText: 'Server Error' });

    expect(await result).toBe(1);
  }, 10000);

  it('does not retry a 4xx error', () => {
    let errorCount = 0;
    httpClient.get('/api/books').subscribe({ error: () => errorCount++ });

    httpMock.expectOne('/api/books').flush('bad request', { status: 400, statusText: 'Bad Request' });

    httpMock.verify();
    expect(errorCount).toBe(1);
  });

  it('never retries a non-GET request', () => {
    let errorCount = 0;
    httpClient.post('/api/v1/subscriptions', {}).subscribe({ error: () => errorCount++ });

    httpMock.expectOne('/api/v1/subscriptions').flush('error', { status: 500, statusText: 'Server Error' });

    httpMock.verify();
    expect(errorCount).toBe(1);
  });
});

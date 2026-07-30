import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { AppUpdateService } from './app-update.service';

describe('AppUpdateService', () => {
  it('is created and init() is a no-op when SwUpdate is not provided (e.g. no provideServiceWorker in the test injector)', () => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });

    const service = TestBed.inject(AppUpdateService);
    expect(service).toBeTruthy();
    expect(() => service.init()).not.toThrow();
    expect(service.updateAvailable()).toBe(false);
  });

  it('applyUpdate() does not throw when SwUpdate is unavailable', () => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });

    const service = TestBed.inject(AppUpdateService);
    expect(() => service.applyUpdate()).not.toThrow();
  });
});

import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { GlobalErrorHandler } from './global-error-handler';
import { ErrorStateService } from '../services/error-state.service';

describe('GlobalErrorHandler', () => {
  let handler: GlobalErrorHandler;
  let errorState: ErrorStateService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), GlobalErrorHandler],
    });
    handler = TestBed.inject(GlobalErrorHandler);
    errorState = TestBed.inject(ErrorStateService);
  });

  it('shows the fatal error screen for an ordinary error', () => {
    handler.handleError(new Error('boom'));

    expect(errorState.state()).toEqual({ kind: 'fatal' });
  });

  it('shows a reload prompt instead of the fatal screen for a chunk load failure', () => {
    handler.handleError(new Error('Loading chunk 3 failed.'));

    const state = errorState.state();
    expect(state.kind).toBe('reload');
  });

  it('handles a non-Error thrown value without crashing', () => {
    expect(() => handler.handleError('a plain string error')).not.toThrow();
    expect(errorState.state()).toEqual({ kind: 'fatal' });
  });
});

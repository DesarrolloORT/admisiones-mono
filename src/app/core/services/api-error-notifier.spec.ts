import { TestBed } from '@angular/core/testing';
import { NormalizedApiError } from '@desarrolloort/ngx-utils';
import { vi } from 'vitest';

import { SnackbarHandler } from '../../shared/ui/snackbar/snackbar-handler';
import { AppApiErrorNotifier } from './api-error-notifier';

describe('AppApiErrorNotifier', () => {
  it('should show normalized API errors with the app snackbar', () => {
    const snackbar = { error: vi.fn() };

    TestBed.configureTestingModule({
      providers: [AppApiErrorNotifier, { provide: SnackbarHandler, useValue: snackbar }],
    });

    const notifier = TestBed.inject(AppApiErrorNotifier);
    const error: NormalizedApiError = {
      status: 400,
      message: 'Los datos enviados no son válidos.',
      action: 'notify',
      isOperationResult: false,
      originalError: new Error('boom'),
    };

    notifier.notify(error);

    expect(snackbar.error).toHaveBeenCalledWith('Los datos enviados no son válidos.');
  });
});

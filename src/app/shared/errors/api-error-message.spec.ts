import type { NormalizedApiError } from '@desarrolloort/ngx-utils';

import { getApiErrorMessage } from './api-error-message';

describe('getApiErrorMessage', () => {
  it('uses the normalized API message when available', () => {
    const error: NormalizedApiError = {
      status: 401,
      message: 'Credenciales inválidas.',
      action: 'notify',
      isOperationResult: false,
      originalError: null,
    };

    expect(getApiErrorMessage(error, 'Fallback')).toBe('Credenciales inválidas.');
  });

  it('falls back for unknown errors', () => {
    expect(getApiErrorMessage(new Error('boom'), 'Fallback')).toBe('Fallback');
    expect(getApiErrorMessage(null, 'Fallback')).toBe('Fallback');
  });
});

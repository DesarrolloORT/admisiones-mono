import { DEFAULT_API_ERROR_POLICY } from '@desarrolloort/ngx-utils';

import { APP_API_ERROR_POLICY } from './api-error-policy';

describe('APP_API_ERROR_POLICY', () => {
  it.each([400, 401, 403, 404, 409, 422, 429])(
    'preserves the default policy and enables the backend message for %i',
    status => {
      expect(APP_API_ERROR_POLICY[status]).toEqual({
        ...DEFAULT_API_ERROR_POLICY[status],
        useBackendMessage: true,
      });
    }
  );

  it('keeps 5xx statuses outside the backend-message policy', () => {
    expect(APP_API_ERROR_POLICY[500]).toBeUndefined();
  });
});

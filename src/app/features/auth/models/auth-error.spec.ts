import { AuthRequestError } from './auth-error';

describe('AuthRequestError', () => {
  it('should expose operation and status', () => {
    const error = new AuthRequestError('login', 401);

    expect(error.operation).toBe('login');
    expect(error.status).toBe(401);
    expect(error.message).toBe('login');
  });
});

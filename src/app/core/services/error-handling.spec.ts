import { vi } from 'vitest';

import { ErrorHandling } from './error-handling';

describe('ErrorHandling', () => {
  let service: ErrorHandling;

  beforeEach(() => {
    service = new ErrorHandling();
  });

  it('should create the service', () => {
    expect(service).toBeTruthy();
  });

  it('should expose handleErrorInUI as a function', () => {
    expect(typeof service.handleErrorInUI).toBe('function');
  });

  it('should log the received error', () => {
    const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const error = new Error('boom');

    service.handleErrorInUI(error);

    expect(consoleErrorSpy).toHaveBeenCalledWith('Unhandled HTTP error', error);
  });
});


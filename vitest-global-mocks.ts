/* eslint-disable @typescript-eslint/no-explicit-any */
import { vi } from 'vitest';

/* Global mocks for jsdom */
const mockStorage = () => {
  let storage: Record<string, string> = {};
  return {
    getItem: (key: string) => (key in storage ? storage[key] : null),
    setItem: (key: string, value: string) => {
      storage[key] = value;
    },
    removeItem: (key: string) => {
      delete storage[key];
    },
    clear: () => {
      storage = {};
    },
  };
};

Object.defineProperty(window, 'localStorage', { value: mockStorage() });
Object.defineProperty(window, 'sessionStorage', { value: mockStorage() });

window.ResizeObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
})) as any;

// jsdom may not allow redefining location depending on runtime internals.
try {
  Object.defineProperty(window, 'location', {
    value: {
      reload: vi.fn(),
      replace: vi.fn(),
      origin: 'http://localhost',
    },
    writable: true,
    configurable: true,
  });
} catch {
  // Ignore when location cannot be redefined.
}

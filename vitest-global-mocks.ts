/* eslint-disable @typescript-eslint/no-explicit-any -- jsdom mocks need loose browser API shapes. */
import { vi } from 'vitest';

/* Global mocks for jsdom */
const mockStorage = () => {
  let storage: Record<string, string> = {};
  return {
    get length() {
      return Object.keys(storage).length;
    },
    key: (index: number) => Object.keys(storage)[index] ?? null,
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
Object.defineProperty(window, 'scrollTo', { value: vi.fn(), writable: true });

if (!HTMLDialogElement.prototype.showModal) {
  Object.defineProperty(HTMLDialogElement.prototype, 'showModal', {
    configurable: true,
    value: vi.fn(function (this: HTMLDialogElement) {
      this.open = true;
    }),
  });
}

if (!HTMLDialogElement.prototype.close) {
  Object.defineProperty(HTMLDialogElement.prototype, 'close', {
    configurable: true,
    value: vi.fn(function (this: HTMLDialogElement) {
      this.open = false;
    }),
  });
}

window.ResizeObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
})) as any;

// jsdom may not allow redefining location depending on runtime internals.
try {
  Object.defineProperty(window, 'location', {
    value: {
      href: 'http://localhost/',
      protocol: 'http:',
      host: 'localhost',
      hostname: 'localhost',
      port: '',
      pathname: '/',
      search: '',
      hash: '',
      reload: vi.fn(),
      replace: vi.fn(),
      origin: 'http://localhost',
      toString: () => 'http://localhost/',
    },
    writable: true,
    configurable: true,
  });
} catch {
  // Ignore when location cannot be redefined.
}

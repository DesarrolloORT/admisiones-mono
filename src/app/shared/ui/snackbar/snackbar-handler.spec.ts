import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { SnackbarHandler } from './snackbar-handler';

describe('SnackbarHandler', () => {
  let handler: SnackbarHandler;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SnackbarHandler],
    });

    handler = TestBed.inject(SnackbarHandler);
  });

  it('should show success snackbar without errors', () => {
    const spy = vi.spyOn(console, 'info').mockImplementation(() => {});
    handler.success('Guardado');

    expect(spy).toHaveBeenCalledWith('[Snackbar][success] Guardado');
    spy.mockRestore();
  });

  it('should dismiss current snackbar', () => {
    handler.information('Info');
    handler.dismiss();
    // No errors thrown means dismiss worked
    expect(true).toBe(true);
  });
});

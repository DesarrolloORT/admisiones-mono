import { TestBed } from '@angular/core/testing';
import { OrtSnackbarService } from '@desarrolloort/components';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from './snackbar-handler';

describe('SnackbarHandler', () => {
  let handler: SnackbarHandler;
  let snackbarMock: {
    open: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    snackbarMock = {
      open: vi.fn().mockReturnValue({
        onAction: () => of(undefined),
        dismiss: vi.fn(),
      }),
    };

    TestBed.configureTestingModule({
      providers: [SnackbarHandler, { provide: OrtSnackbarService, useValue: snackbarMock }],
    });

    handler = TestBed.inject(SnackbarHandler);
  });

  it('should open success snackbars through ORT snackbar service', async () => {
    handler.success('Guardado');
    await Promise.resolve();

    expect(snackbarMock.open).toHaveBeenCalledWith(
      expect.objectContaining({
        message: 'Guardado',
        variant: 'success',
      })
    );
  });

  it('should dismiss current snackbar', async () => {
    const ref = { onAction: () => of(undefined), dismiss: vi.fn() };
    snackbarMock.open.mockReturnValue(ref);

    handler.information('Info');
    await Promise.resolve();
    handler.dismiss();

    expect(ref.dismiss).toHaveBeenCalled();
  });
});

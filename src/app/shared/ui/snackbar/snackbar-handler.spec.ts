import { TestBed } from '@angular/core/testing';
import { OrtSnackbarService } from '@desarrolloort/components';
import { EMPTY } from 'rxjs';
import { vi } from 'vitest';

import { SnackbarHandler } from './snackbar-handler';

describe('SnackbarHandler', () => {
  let handler: SnackbarHandler;
  let openMock: ReturnType<typeof vi.fn>;
  let dismissMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    dismissMock = vi.fn();
    openMock = vi.fn().mockReturnValue({
      dismiss: dismissMock,
      onAction: () => EMPTY,
    });

    TestBed.configureTestingModule({
      providers: [
        SnackbarHandler,
        {
          provide: OrtSnackbarService,
          useValue: { open: openMock },
        },
      ],
    });

    handler = TestBed.inject(SnackbarHandler);
  });

  it('should show success snackbar without errors', async () => {
    handler.success('Guardado');
    await Promise.resolve();

    expect(openMock).toHaveBeenCalledWith(
      expect.objectContaining({
        message: 'Guardado',
        variant: 'success',
      })
    );
  });

  it('should dismiss current snackbar', async () => {
    handler.information('Info');
    await Promise.resolve();

    handler.dismiss();

    expect(dismissMock).toHaveBeenCalled();
  });
});

import { TestBed } from '@angular/core/testing';

import { InscripcionDialog } from './inscripcion-dialog';

describe('InscripcionDialog', () => {
  it('creates', () => {
    TestBed.configureTestingModule({ imports: [InscripcionDialog] }).overrideComponent(
      InscripcionDialog,
      { set: { imports: [], template: '' } }
    );

    expect(TestBed.createComponent(InscripcionDialog).componentInstance).toBeTruthy();
  });
});

import { TestBed } from '@angular/core/testing';

import { InscriptionResumeContextStore } from './inscription-resume-context';

describe('InscriptionResumeContextStore', () => {
  let store: InscriptionResumeContextStore;

  beforeEach(() => {
    sessionStorage.clear();
    store = TestBed.inject(InscriptionResumeContextStore);
  });

  it('stores only unique positive ids for the selected inscription', () => {
    store.save({
      idProducto: 40,
      idProceso: 210,
      idOfertas: [310, 311, 310, 0],
      idInscripciones: [7010, 7011, 7010, -1],
    });

    expect(store.read(40, 210)).toEqual({
      idProducto: 40,
      idProceso: 210,
      idOfertas: [310, 311],
      idInscripciones: [7010, 7011],
    });
    expect(store.read(41, 210)).toBeNull();
  });

  it('ignores malformed session data', () => {
    sessionStorage.setItem('inscription-resume-context', '{invalid');

    expect(store.read(40, 210)).toBeNull();
  });
});

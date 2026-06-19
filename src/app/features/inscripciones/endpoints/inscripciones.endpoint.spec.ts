/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { TestBed } from '@angular/core/testing';
import { InscripcionesEndpoint } from 'inscripciones.endpoint';

describe('InscripcionesEndpoint', () => {
  let service: InscripcionesEndpoint;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [InscripcionesEndpoint],
    });
    service = TestBed.inject(InscripcionesEndpoint);
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

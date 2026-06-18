/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { TestBed } from '@angular/core/testing';
import { Inscripciones } from 'inscripciones';

describe('Inscripciones', () => {
  let service: Inscripciones;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [Inscripciones],
    });
    service = TestBed.inject(Inscripciones);
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

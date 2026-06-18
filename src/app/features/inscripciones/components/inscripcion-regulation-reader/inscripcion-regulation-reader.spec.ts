/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InscripcionRegulationReader } from 'inscripcion-regulation-reader';

describe('InscripcionRegulationReader', () => {
  let component: InscripcionRegulationReader;
  let fixture: ComponentFixture<InscripcionRegulationReader>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InscripcionRegulationReader],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(InscripcionRegulationReader);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

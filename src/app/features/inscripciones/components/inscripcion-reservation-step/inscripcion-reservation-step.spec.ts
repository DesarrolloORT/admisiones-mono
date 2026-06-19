/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InscripcionReservationStep } from 'inscripcion-reservation-step';

describe('InscripcionReservationStep', () => {
  let component: InscripcionReservationStep;
  let fixture: ComponentFixture<InscripcionReservationStep>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InscripcionReservationStep],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(InscripcionReservationStep);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

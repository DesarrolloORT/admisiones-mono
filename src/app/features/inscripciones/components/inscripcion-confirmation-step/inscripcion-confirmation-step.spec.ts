/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InscripcionConfirmationStep } from 'inscripcion-confirmation-step';

describe('InscripcionConfirmationStep', () => {
  let component: InscripcionConfirmationStep;
  let fixture: ComponentFixture<InscripcionConfirmationStep>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InscripcionConfirmationStep],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(InscripcionConfirmationStep);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

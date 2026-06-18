/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InscripcionSuccessStep } from 'inscripcion-success-step';

describe('InscripcionSuccessStep', () => {
  let component: InscripcionSuccessStep;
  let fixture: ComponentFixture<InscripcionSuccessStep>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InscripcionSuccessStep],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(InscripcionSuccessStep);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

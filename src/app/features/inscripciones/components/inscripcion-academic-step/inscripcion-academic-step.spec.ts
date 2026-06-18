/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InscripcionAcademicStep } from 'inscripcion-academic-step';

describe('InscripcionAcademicStep', () => {
  let component: InscripcionAcademicStep;
  let fixture: ComponentFixture<InscripcionAcademicStep>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InscripcionAcademicStep],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(InscripcionAcademicStep);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

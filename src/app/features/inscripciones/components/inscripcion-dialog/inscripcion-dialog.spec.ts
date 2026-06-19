/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InscripcionDialog } from 'inscripcion-dialog';

describe('InscripcionDialog', () => {
  let component: InscripcionDialog;
  let fixture: ComponentFixture<InscripcionDialog>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InscripcionDialog],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(InscripcionDialog);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProcessLayout } from 'process-layout';

describe('ProcessLayout', () => {
  let component: ProcessLayout;
  let fixture: ComponentFixture<ProcessLayout>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProcessLayout],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ProcessLayout);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

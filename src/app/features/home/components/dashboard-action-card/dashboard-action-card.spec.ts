/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActionCardVariant } from 'dashboard-action-card';

describe('ActionCardVariant', () => {
  let component: ActionCardVariant;
  let fixture: ComponentFixture<ActionCardVariant>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ActionCardVariant],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ActionCardVariant);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

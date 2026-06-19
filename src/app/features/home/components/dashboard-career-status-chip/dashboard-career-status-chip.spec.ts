/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardCareerStatusChip } from 'dashboard-career-status-chip';

describe('DashboardCareerStatusChip', () => {
  let component: DashboardCareerStatusChip;
  let fixture: ComponentFixture<DashboardCareerStatusChip>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardCareerStatusChip],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(DashboardCareerStatusChip);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

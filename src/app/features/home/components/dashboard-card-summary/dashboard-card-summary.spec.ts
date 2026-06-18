/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardCardSummary } from 'dashboard-card-summary';

describe('DashboardCardSummary', () => {
  let component: DashboardCardSummary;
  let fixture: ComponentFixture<DashboardCardSummary>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardCardSummary],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(DashboardCardSummary);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

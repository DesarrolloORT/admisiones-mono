/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardCard } from 'dashboard-card';

describe('DashboardCard', () => {
  let component: DashboardCard;
  let fixture: ComponentFixture<DashboardCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardCard],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(DashboardCard);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

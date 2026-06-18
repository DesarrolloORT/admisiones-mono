/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardScholarshipsSection } from 'dashboard-scholarships-section';

describe('DashboardScholarshipsSection', () => {
  let component: DashboardScholarshipsSection;
  let fixture: ComponentFixture<DashboardScholarshipsSection>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardScholarshipsSection],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(DashboardScholarshipsSection);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

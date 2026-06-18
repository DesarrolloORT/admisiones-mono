/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { TestBed } from '@angular/core/testing';
import { DashboardService } from 'dashboard';

describe('DashboardService', () => {
  let service: DashboardService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [DashboardService],
    });
    service = TestBed.inject(DashboardService);
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

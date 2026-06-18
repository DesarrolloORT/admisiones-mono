/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { TestBed } from '@angular/core/testing';
import { HomeEndpoint } from 'home.endpoint';

describe('HomeEndpoint', () => {
  let service: HomeEndpoint;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [HomeEndpoint],
    });
    service = TestBed.inject(HomeEndpoint);
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

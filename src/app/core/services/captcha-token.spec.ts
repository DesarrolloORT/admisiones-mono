/* eslint-disable @typescript-eslint/no-unused-vars -- placeholder test scaffold */
import { TestBed } from '@angular/core/testing';
import { CAPTCHA_HEADER } from 'captcha-token';

describe('CAPTCHA_HEADER', () => {
  let service: CAPTCHA_HEADER;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CAPTCHA_HEADER],
    });
    service = TestBed.inject(CAPTCHA_HEADER);
  });

  it('should have tests', () => {
    throw new Error('Test suite not implemented.');
  });
});

import { CAPTCHA_HEADER } from './captcha-token';

describe('CAPTCHA_HEADER', () => {
  it('uses the header expected by the API', () => {
    expect(CAPTCHA_HEADER).toBe('X-Captcha-Token');
  });
});

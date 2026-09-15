import { App } from './app';

describe('App', () => {
  let subject: App;

  beforeEach(() => {
    subject = new App();
  });

  it('should create the shell', () => {
    expect(subject).toBeTruthy();
  });
});

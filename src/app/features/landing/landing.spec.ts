import { Landing } from './landing';

describe('Landing', () => {
  let subject: Landing;

  beforeEach(() => {
    subject = new Landing();
  });

  it('should create the feature', () => {
    expect(subject).toBeTruthy();
  });

  it('should expose the current year', () => {
    expect(subject['currentYear']()).toBe(new Date().getFullYear());
  });

  it('should expose the feature cards', () => {
    const features = subject['features']();

    expect(features).toHaveLength(6);
    expect(features[0]).toMatchObject({
      title: 'Bibliotecas Integradas',
      icon: 'package_2',
    });
  });

  it('should expose the onboarding steps', () => {
    const tourSteps = subject['tourSteps']();

    expect(tourSteps).toHaveLength(4);
    expect(tourSteps[0]?.code).toContain('git clone');
  });
});

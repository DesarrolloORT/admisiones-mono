import { HomeDashboard } from './home-dashboard';

describe('HomeDashboard', () => {
  it('should represent home actions', () => {
    const dashboard: HomeDashboard = {
      userName: 'Ana',
      description: 'Descripción',
      actionCards: [
        {
          id: 'career',
          title: 'Inscripción',
          description: 'Comenzar',
          icon: 'school',
          ctaLabel: 'Ir',
          imageSrc: 'asset.png',
        },
      ],
    };

    expect(dashboard.actionCards[0]?.id).toBe('career');
  });
});

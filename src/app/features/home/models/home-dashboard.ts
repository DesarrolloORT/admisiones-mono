export interface HomeActionCard {
  id: string;
  title: string;
  description: string;
  icon: string;
  ctaLabel: string;
  imageSrc?: string;
  route?: string;
  disabledReason?: string;
}

export interface HomeDashboard {
  userName: string;
  description: string;
  actionCards: HomeActionCard[];
}

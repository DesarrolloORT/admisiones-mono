import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AuthSession } from '../../../auth/models/auth.interface';
import { AuthSessionService } from '../../../auth/services/auth-session';
import { HomeLayout } from './home-layout';

describe('HomeLayout', () => {
  let fixture: ComponentFixture<HomeLayout>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HomeLayout],
      providers: [
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: {
            logout: vi.fn(),
            session: signal<AuthSession | null>(null),
          },
        },
      ],
    });

    fixture = TestBed.createComponent(HomeLayout);
  });

  it('should render the shared header and the routed content outlet', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-home-header')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('router-outlet')).toBeTruthy();
  });
});

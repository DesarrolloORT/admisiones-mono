import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthSession } from '../../../auth/models/auth.interface';
import { AuthSessionService } from '../../../auth/services/auth-session';
import { HomeEndpoint } from '../../endpoints/home.endpoint';
import { Home } from './home';

describe('Home', () => {
  let fixture: ComponentFixture<Home>;
  let component: Home;
  let authMock: {
    logout: ReturnType<typeof vi.fn>;
    session: ReturnType<typeof signal<AuthSession | null>>;
  };

  beforeEach(() => {
    authMock = {
      logout: vi.fn(),
      session: signal<AuthSession | null>({
        token: null,
        documentType: 'CI',
        documentNumber: '12345678',
        primerNombre: 'Ana',
        expiresAt: null,
      }),
    };

    TestBed.configureTestingModule({
      imports: [Home],
      providers: [
        provideRouter([]),
        { provide: AuthSessionService, useValue: authMock },
        { provide: HomeEndpoint, useValue: { getMisInscripciones: () => of([]) } },
      ],
    });

    fixture = TestBed.createComponent(Home);
    component = fixture.componentInstance;
  });

  it('should render the current user name and primary actions', () => {
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;

    expect(component).toBeTruthy();
    expect(text).toContain('¡Hola Ana!');
    expect(text).toContain('Comenzar inscripción');
    expect(text).toContain('Postularme a beca');
    expect(text).not.toContain('Mis carreras');
    expect(text).not.toContain('Mis becas');

    const primaryLinks = Array.from(
      fixture.nativeElement.querySelectorAll('a.home-action-card__primary')
    ) as HTMLAnchorElement[];

    expect(primaryLinks.map(link => link.getAttribute('href'))).toEqual(['/inscripciones', '/becas']);
  });
});

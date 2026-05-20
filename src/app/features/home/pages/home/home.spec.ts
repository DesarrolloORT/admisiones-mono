import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AuthSession } from '../../../auth/models/auth.interface';
import { Auth } from '../../../auth/services/auth';
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
      providers: [{ provide: Auth, useValue: authMock }],
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
  });
});

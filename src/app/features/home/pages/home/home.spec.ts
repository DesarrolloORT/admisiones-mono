import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AuthSession } from '../../../auth/models/auth.interface';
import { AuthSessionService } from '../../../auth/services/auth-session';
import { HomeData } from '../../models/home-data';
import { Home } from './home';

describe('Home', () => {
  let fixture: ComponentFixture<Home>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Home],
      providers: [
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: {
            logout: vi.fn(),
            session: signal<AuthSession | null>({
              documentType: 'CI',
              documentNumber: '12345678',
              primerNombre: 'Ana',
            }),
          },
        },
      ],
    });
  });

  it('should render the current user name and primary actions', async () => {
    fixture = createComponent({ inscripciones: [], becas: [] });
    await fixture.whenStable();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('¡Hola Ana!');
    expect(text).toContain('Comenzar inscripción');
    expect(text).toContain('Postularme a beca');
  });

  it('should render Dashboard when the user has activity', async () => {
    fixture = createComponent({
      inscripciones: [
        {
          idInscripto: 100,
          idProducto: 1,
          idProceso: 4,
          idComienzo: 2,
          idTurno: 3,
          nombreProducto: 'Analista Programador',
          nombreComienzo: 'Marzo 2027',
          nombreTurno: 'Noche',
          estado: 'Confirmada',
        },
      ],
      becas: [],
    });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('app-dashboard')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Analista Programador');
    expect(fixture.nativeElement.querySelector('.home-actions')).toBeNull();
  });

  it('should keep request failures distinct from an empty account', async () => {
    fixture = createComponent(null);
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('No pudimos cargar tu información');
    expect(fixture.nativeElement.querySelector('app-dashboard')).toBeNull();
    expect(fixture.nativeElement.querySelector('.home-actions')).toBeNull();
  });

  function createComponent(homeData: HomeData | null): ComponentFixture<Home> {
    const componentFixture = TestBed.createComponent(Home);
    componentFixture.componentRef.setInput('homeData', homeData);
    return componentFixture;
  }
});

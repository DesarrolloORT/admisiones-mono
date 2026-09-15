import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AuthSession } from '../../../auth/models/auth.interface';
import { AuthSessionService } from '../../../auth/services/auth-session';
import { HomeResolved } from '../../models/home-data';
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
              firstName: 'Ana',
            }),
          },
        },
      ],
    });
  });

  it('should render the current user name and primary actions', async () => {
    fixture = createComponent({ enrollments: [], scholarships: [] });
    await fixture.whenStable();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('¡Hola Ana!');
    expect(text).toContain('Comenzar inscripción');
    expect(text).toContain('Postularme a beca');
  });

  it('should render Dashboard when the user has activity', async () => {
    fixture = createComponent({
      enrollments: [
        {
          enrollmentId: 100,
          offeringIds: [300],
          productId: 1,
          admissionProcessId: 4,
          productLevelId: 1,
          intakeId: 2,
          shiftId: 3,
          degreeProgramName: 'Analista Programador',
          intakeName: 'Marzo 2027',
          shiftName: 'Noche',
          status: 'Confirmada',
          paymentDueDate: null,
          seminars: [],
        },
      ],
      scholarships: [],
    });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('app-dashboard')).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Analista Programador');
    expect(fixture.nativeElement.querySelector('.home-actions')).toBeNull();
  });

  it('should keep request failures distinct from an empty account', async () => {
    fixture = createComponent({ loadError: 'Hubo un error al cargar tu información.' });
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('No pudimos cargar tu información');
    expect(fixture.nativeElement.querySelector('app-dashboard')).toBeNull();
    expect(fixture.nativeElement.querySelector('.home-actions')).toBeNull();
  });

  it('should render the backend message when the load fails', async () => {
    fixture = createComponent({ loadError: 'Tu usuario no tiene inscripciones habilitadas.' });
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain(
      'Tu usuario no tiene inscripciones habilitadas.'
    );
    expect(fixture.nativeElement.textContent).not.toContain(
      'Hubo un error al cargar tu información.'
    );
  });

  function createComponent(homeData: HomeResolved): ComponentFixture<Home> {
    const componentFixture = TestBed.createComponent(Home);
    componentFixture.componentRef.setInput('homeData', homeData);
    return componentFixture;
  }
});

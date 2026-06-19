import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AuthSession } from '../../../auth/models/auth.interface';
import { AuthSessionService } from '../../../auth/services/auth-session';
import { Dashboard } from './dashboard';

describe('Dashboard', () => {
  let fixture: ComponentFixture<Dashboard>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [
        {
          provide: AuthSessionService,
          useValue: {
            logout: vi.fn(),
            session: signal<AuthSession | null>({
              token: null,
              documentType: 'CI',
              documentNumber: '12345678',
              primerNombre: 'Ana',
              expiresAt: null,
            }),
          },
        },
      ],
    });

    fixture = TestBed.createComponent(Dashboard);
    fixture.componentRef.setInput('inscripciones', [
      {
        idProducto: 1,
        idComienzo: 2,
        idTurno: 3,
        nombreProducto: 'Analista Programador',
        nombreComienzo: 'Marzo 2027',
        nombreTurno: 'Noche',
        estado: 'Confirmada',
      },
    ]);
    fixture.componentRef.setInput('becas', [
      {
        id: 4,
        nombreBeca: 'Fondo de Excelencia Académica',
        nombreCarrera: 'Analista Programador',
        estado: 'En proceso',
        cierrePostulacion: 'Miércoles 15/07/2026',
        fechaPrueba: 'Miércoles 22/07/2026',
        resultadoPrueba: '',
        beneficio: '',
        fechaResultados: '',
      },
    ]);
  });

  it('should render the collections supplied by the home entry point', async () => {
    await fixture.whenStable();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('¡Hola Ana!');
    expect(text).toContain('Mis carreras');
    expect(text).toContain('Analista Programador');
    expect(text).toContain('Mis becas');
    expect(text).toContain('Fondo de Excelencia Académica');
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { ScholarshipsApi } from '../../api/scholarships.api';
import { Scholarships } from './scholarships';

const CATALOGUE = [
  {
    scholarshipTypeIds: [],
    name: 'Fondo de becas de reválidas',
    description: 'Para quienes revalidan materias.',
    requiresTest: false,
  },
  {
    scholarshipTypeIds: [33, 57],
    name: 'Fondo de Excelencia Académica',
    description: 'Para quienes se destacaron en secundaria.',
    requiresTest: true,
  },
];

describe('Scholarships', () => {
  let fixture: ComponentFixture<Scholarships>;
  let getAvailableScholarships: ReturnType<typeof vi.fn>;

  function setup(): void {
    TestBed.configureTestingModule({
      imports: [Scholarships],
      providers: [
        provideRouter([]),
        { provide: ScholarshipsApi, useValue: { getAvailableScholarships } },
      ],
    });

    fixture = TestBed.createComponent(Scholarships);
    fixture.detectChanges();
  }

  function textContent(): string {
    return fixture.nativeElement.textContent as string;
  }

  beforeEach(() => {
    getAvailableScholarships = vi
      .fn()
      .mockReturnValue(of({ requiresPriorEnrollment: true, scholarships: CATALOGUE }));
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('renders the catalogue returned by the API', () => {
    setup();

    expect(textContent()).toContain('Postulación a becas');
    // Los textos son del backend: la page ya no tiene un array propio.
    expect(textContent()).toContain('Fondo de becas de reválidas');
    expect(textContent()).toContain('Para quienes se destacaron en secundaria.');
  });

  it('gates the scholarships that need a prior enrollment', () => {
    setup();

    expect(textContent()).toContain('Con inscripción previa');
    // Reválidas no exige inscripción: sigue siendo postulable.
    const links = fixture.nativeElement.querySelectorAll('a[href="/becas/fbr"]');
    expect(links.length).toBe(1);
  });

  it('opens every scholarship when the person already has an enabling enrollment', () => {
    getAvailableScholarships = vi
      .fn()
      .mockReturnValue(of({ requiresPriorEnrollment: false, scholarships: CATALOGUE }));
    setup();

    expect(textContent()).not.toContain('Con inscripción previa');
    expect(fixture.nativeElement.querySelectorAll('a[href^="/becas/"]').length).toBe(2);
  });

  it('shows a scholarship the front cannot open yet, but without an action', () => {
    getAvailableScholarships = vi.fn().mockReturnValue(
      of({
        requiresPriorEnrollment: false,
        scholarships: [
          {
            scholarshipTypeIds: [9999],
            name: 'Fondo nuevo sin pantalla',
            description: 'Todavía no tiene proceso en el front.',
            requiresTest: false,
          },
        ],
      })
    );
    setup();

    expect(textContent()).toContain('Fondo nuevo sin pantalla');
    expect(textContent()).toContain('Postulación no disponible en línea');
    expect(fixture.nativeElement.querySelectorAll('a[href^="/becas/"]').length).toBe(0);
  });

  it('shows an empty state when the catalogue comes back empty', () => {
    getAvailableScholarships = vi
      .fn()
      .mockReturnValue(of({ requiresPriorEnrollment: true, scholarships: [] }));
    setup();

    expect(textContent()).toContain('No hay becas disponibles en este momento.');
  });

  it('falls back to the empty state when the request fails', () => {
    getAvailableScholarships = vi.fn().mockReturnValue(throwError(() => new Error('boom')));
    setup();

    expect(textContent()).toContain('No hay becas disponibles en este momento.');
  });
});

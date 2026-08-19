import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { ScholarshipsApi } from '../../api/scholarships.api';
import { Scholarships } from './scholarships';

describe('Scholarships', () => {
  let fixture: ComponentFixture<Scholarships>;
  let getConfirmedEnrollments: ReturnType<typeof vi.fn>;

  function setup(): void {
    TestBed.configureTestingModule({
      imports: [Scholarships],
      providers: [
        provideRouter([]),
        { provide: ScholarshipsApi, useValue: { getConfirmedEnrollments } },
      ],
    });

    fixture = TestBed.createComponent(Scholarships);
  }

  beforeEach(() => {
    getConfirmedEnrollments = vi.fn().mockReturnValue(of([]));
    setup();
  });

  it('renders the scholarships catalogue', () => {
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Admisiones');
    expect(text).toContain('Postulación a becas');
    expect(text).toContain('Beca de Reválidas');
  });

  it('marks the person as enrolled when the API returns confirmed enrollments', () => {
    TestBed.resetTestingModule();
    getConfirmedEnrollments = vi.fn().mockReturnValue(of([{ enrollmentId: 1072704 }]));
    setup();

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent as string).not.toContain('Con inscripción previa');
  });

  it('falls back to not enrolled when the request fails', () => {
    TestBed.resetTestingModule();
    getConfirmedEnrollments = vi.fn().mockReturnValue(throwError(() => new Error('boom')));
    setup();

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent as string).toContain('Con inscripción previa');
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ScholarshipCard, type ScholarshipCardModel } from './scholarship-card';

const BASE: ScholarshipCardModel = {
  title: 'Fondo de becas de reválidas',
  description: 'Para quienes revalidan materias.',
  requiresExam: false,
  requiresEnrollment: false,
  route: '/becas/fbr',
};

describe('ScholarshipCard', () => {
  let fixture: ComponentFixture<ScholarshipCard>;

  function setup(scholarship: ScholarshipCardModel, isEnrolled = false): void {
    TestBed.configureTestingModule({
      imports: [ScholarshipCard],
      providers: [provideRouter([])],
    });

    fixture = TestBed.createComponent(ScholarshipCard);
    fixture.componentRef.setInput('scholarship', scholarship);
    fixture.componentRef.setInput('isEnrolled', isEnrolled);
    fixture.detectChanges();
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('links to the scholarship process when the front knows the route', () => {
    setup(BASE);

    expect(fixture.nativeElement.querySelector('a[href="/becas/fbr"]')).not.toBeNull();
  });

  it('offers no action when the route is unknown', () => {
    setup({ ...BASE, route: null });

    expect(fixture.nativeElement.querySelector('a')).toBeNull();
    expect(fixture.nativeElement.textContent as string).toContain(
      'Postulación no disponible en línea'
    );
  });

  it('sends the person to enrollments when the scholarship needs one', () => {
    setup({ ...BASE, requiresEnrollment: true, route: '/becas/fexa' });

    expect(fixture.nativeElement.querySelector('a[href="/inscripciones"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('a[href="/becas/fexa"]')).toBeNull();
  });

  it('derives a unique id per card so aria-labelledby points at its own title', () => {
    setup(BASE);

    const article = fixture.nativeElement.querySelector('article') as HTMLElement;
    const heading = fixture.nativeElement.querySelector('h2') as HTMLElement;

    expect(heading.id).toBe('scholarship-card-title-fondo-de-becas-de-revalidas');
    expect(article.getAttribute('aria-labelledby')).toBe(heading.id);
  });
});

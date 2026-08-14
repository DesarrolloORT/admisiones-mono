import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { Scholarships } from './scholarships';

describe('Scholarships', () => {
  let fixture: ComponentFixture<Scholarships>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Scholarships],
      providers: [provideRouter([])],
    });

    fixture = TestBed.createComponent(Scholarships);
  });

  it('renders the scholarships catalogue', () => {
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Admisiones');
    expect(text).toContain('Postulación a becas');
    expect(text).toContain('Beca de Reválidas');
  });
});

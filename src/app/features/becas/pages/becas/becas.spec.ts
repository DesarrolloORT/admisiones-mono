import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { Becas } from './becas';

describe('Becas', () => {
  let fixture: ComponentFixture<Becas>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Becas],
      providers: [provideRouter([])],
    });

    fixture = TestBed.createComponent(Becas);
  });

  it('renders the scholarships catalogue', () => {
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Admisiones');
    expect(text).toContain('Postulación a becas');
    expect(text).toContain('Beca de Reválidas');
  });
});

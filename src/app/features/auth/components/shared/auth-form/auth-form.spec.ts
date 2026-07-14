import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AuthForm } from './auth-form';

describe('AuthForm', () => {
  let fixture: ComponentFixture<AuthForm>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AuthForm],
    });

    fixture = TestBed.createComponent(AuthForm);
    fixture.componentRef.setInput('title', 'Crear cuenta');
    fixture.componentRef.setInput('description', 'El registro te llevará solo unos minutos.');
    fixture.componentRef.setInput('heroTitle', 'Proyección global.');
    fixture.componentRef.setInput('heroDescription', 'Validá tu talento.');
    fixture.componentRef.setInput('heroIcon', 'public');
    fixture.detectChanges();
  });

  it('should create the shared auth layout', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the title and description', () => {
    const element = fixture.nativeElement as HTMLElement;

    expect(element.textContent).toContain('Crear cuenta');
    expect(element.textContent).toContain('El registro te llevará solo unos minutos.');
  });
});

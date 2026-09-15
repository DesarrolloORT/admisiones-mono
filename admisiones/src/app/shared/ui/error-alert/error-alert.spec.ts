import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DEFAULT_ERROR_ALERT, ErrorAlert } from './error-alert';

describe('ErrorAlert', () => {
  let fixture: ComponentFixture<ErrorAlert>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ErrorAlert] });
    fixture = TestBed.createComponent(ErrorAlert);
  });

  it('renders the default alert copy', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(DEFAULT_ERROR_ALERT.title);
    expect(fixture.nativeElement.textContent).toContain(DEFAULT_ERROR_ALERT.message);
  });

  it('renders custom alert copy', () => {
    fixture.componentRef.setInput('title', 'Banco requerido');
    fixture.componentRef.setInput('message', 'Seleccioná tu banco para poder continuar.');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Banco requerido');
    expect(fixture.nativeElement.textContent).toContain('Seleccioná tu banco');
  });
});

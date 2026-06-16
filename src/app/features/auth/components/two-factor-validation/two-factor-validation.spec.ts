import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TwoFactorValidation } from './two-factor-validation';

describe('TwoFactorValidation', () => {
  let fixture: ComponentFixture<TwoFactorValidation>;
  let component: TwoFactorValidation;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [TwoFactorValidation] });

    fixture = TestBed.createComponent(TwoFactorValidation);
    fixture.componentRef.setInput('email', 'john.doe@example.com');
    fixture.detectChanges();
    component = fixture.componentInstance;
  });

  it('renders the title, masked email and six digit inputs', () => {
    const element = fixture.nativeElement as HTMLElement;

    expect(element.textContent).toContain('Autenticación requerida');
    expect(element.textContent).toContain('john.doe@example.com');
    expect(element.querySelectorAll('input.digit').length).toBe(6);
  });

  it('emits confirm when the user pastes six digits', () => {
    const element = fixture.nativeElement as HTMLElement;
    const inputs = element.querySelectorAll<HTMLInputElement>('input.digit');
    const emitSpy = vi.spyOn(component.confirm, 'emit');

    const pasteEvent = new Event('paste', { bubbles: true, cancelable: true });
    Object.defineProperty(pasteEvent, 'clipboardData', {
      value: { getData: () => '123456' },
    });
    inputs[0].dispatchEvent(pasteEvent);
    fixture.detectChanges();

    expect(emitSpy).toHaveBeenCalledWith('123456');
  });

  it('emits resend when the resend link is clicked', () => {
    const element = fixture.nativeElement as HTMLElement;
    const emitSpy = vi.spyOn(component.resend, 'emit');
    const link = element.querySelector<HTMLButtonElement>('.resend-link');

    link?.click();

    expect(emitSpy).toHaveBeenCalled();
  });

  it('does not emit resend while submitting', () => {
    fixture.componentRef.setInput('isSubmitting', true);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const emitSpy = vi.spyOn(component.resend, 'emit');
    const link = element.querySelector<HTMLButtonElement>('.resend-link');

    link?.click();

    expect(emitSpy).not.toHaveBeenCalled();
  });
});

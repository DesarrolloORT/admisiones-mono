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

  describe('handleInput', () => {
    it('advances focus to the next input when a digit is typed', () => {
      const element = fixture.nativeElement as HTMLElement;
      const inputs = element.querySelectorAll<HTMLInputElement>('input.digit');
      const focusSpy = vi.spyOn(inputs[1], 'focus');

      inputs[0].value = '1';
      inputs[0].dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(focusSpy).toHaveBeenCalled();
    });

    it('filters out non-digit characters', () => {
      const element = fixture.nativeElement as HTMLElement;
      const inputs = element.querySelectorAll<HTMLInputElement>('input.digit');

      inputs[0].value = 'a';
      inputs[0].dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component['form'].at(0).value).toBe('');
      expect(inputs[0].value).toBe('');
    });

    it('auto-submits when the sixth digit completes the code', () => {
      const element = fixture.nativeElement as HTMLElement;
      const inputs = element.querySelectorAll<HTMLInputElement>('input.digit');
      const emitSpy = vi.spyOn(component.confirm, 'emit');

      for (let i = 0; i < 5; i += 1) {
        inputs[i].value = `${i + 1}`;
        inputs[i].dispatchEvent(new Event('input'));
        fixture.detectChanges();
      }

      expect(emitSpy).not.toHaveBeenCalled();

      inputs[5].value = '6';
      inputs[5].dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(emitSpy).toHaveBeenCalledWith('123456');
    });
  });

  describe('handleKeydown', () => {
    it('moves focus to the previous input on Backspace when the current input is empty', () => {
      const element = fixture.nativeElement as HTMLElement;
      const inputs = element.querySelectorAll<HTMLInputElement>('input.digit');
      const focusSpy = vi.spyOn(inputs[0], 'focus');

      const event = new KeyboardEvent('keydown', { key: 'Backspace', cancelable: true });
      inputs[1].dispatchEvent(event);
      fixture.detectChanges();

      expect(focusSpy).toHaveBeenCalled();
    });

    it('moves focus left on ArrowLeft', () => {
      const element = fixture.nativeElement as HTMLElement;
      const inputs = element.querySelectorAll<HTMLInputElement>('input.digit');
      const focusSpy = vi.spyOn(inputs[0], 'focus');

      const event = new KeyboardEvent('keydown', { key: 'ArrowLeft', cancelable: true });
      inputs[1].dispatchEvent(event);
      fixture.detectChanges();

      expect(focusSpy).toHaveBeenCalled();
    });

    it('moves focus right on ArrowRight', () => {
      const element = fixture.nativeElement as HTMLElement;
      const inputs = element.querySelectorAll<HTMLInputElement>('input.digit');
      const focusSpy = vi.spyOn(inputs[1], 'focus');

      const event = new KeyboardEvent('keydown', { key: 'ArrowRight', cancelable: true });
      inputs[0].dispatchEvent(event);
      fixture.detectChanges();

      expect(focusSpy).toHaveBeenCalled();
    });
  });

  it('fills and focuses the next empty input when pasting fewer than six digits', () => {
    const element = fixture.nativeElement as HTMLElement;
    const inputs = element.querySelectorAll<HTMLInputElement>('input.digit');
    const focusSpy = vi.spyOn(inputs[3], 'focus');
    const emitSpy = vi.spyOn(component.confirm, 'emit');

    const pasteEvent = new Event('paste', { bubbles: true, cancelable: true });
    Object.defineProperty(pasteEvent, 'clipboardData', {
      value: { getData: () => '123' },
    });
    inputs[0].dispatchEvent(pasteEvent);
    fixture.detectChanges();

    expect(component['form'].getRawValue()).toEqual(['1', '2', '3', '', '', '']);
    expect(focusSpy).toHaveBeenCalled();
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('marks all fields as touched and does not emit submit when the form is invalid', () => {
    const emitSpy = vi.spyOn(component.confirm, 'emit');

    component['submitFromButton']();

    expect(component['form'].touched).toBe(true);
    component['form'].controls.forEach(control => expect(control.touched).toBe(true));
    expect(emitSpy).not.toHaveBeenCalled();
  });
});

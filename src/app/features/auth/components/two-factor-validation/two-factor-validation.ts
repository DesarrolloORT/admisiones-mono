import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  output,
  viewChildren,
} from '@angular/core';
import { FormArray, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

const CODE_LENGTH = 6;
const DIGIT_PATTERN = /^\d$/;
const ALL_DIGITS_PATTERN = /^\d+$/;

@Component({
  selector: 'app-two-factor-validation',
  imports: [ReactiveFormsModule, OrtButtonModule, OrtIconModule],
  templateUrl: './two-factor-validation.html',
  styleUrl: './two-factor-validation.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TwoFactorValidation {
  protected readonly codeLength = CODE_LENGTH;
  protected readonly slots = Array.from({ length: CODE_LENGTH });

  public readonly email = input.required<string>();
  public readonly isSubmitting = input<boolean>(false);
  public readonly error = input<string | null>(null);

  public readonly confirm = output<string>();
  public readonly resend = output<void>();

  protected readonly form = new FormArray(
    Array.from(
      { length: CODE_LENGTH },
      () =>
        new FormControl('', {
          nonNullable: true,
          validators: [Validators.required, Validators.pattern(DIGIT_PATTERN)],
        })
    )
  );

  private readonly digitInputs = viewChildren<ElementRef<HTMLInputElement>>('digitInput');

  constructor() {
    afterNextRender(() => this.focusInput(0));
  }

  protected handleInput(index: number, event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    const digit = inputEl.value.replace(/\D/g, '').slice(-1);

    this.form.at(index).setValue(digit);
    inputEl.value = digit;

    if (digit && index < CODE_LENGTH - 1) {
      this.focusInput(index + 1);
    }

    this.maybeSubmit();
  }

  protected handleKeydown(index: number, event: KeyboardEvent): void {
    if (event.key === 'Backspace') {
      const current = this.form.at(index).value;
      if (!current && index > 0) {
        event.preventDefault();
        this.form.at(index - 1).setValue('');
        this.focusInput(index - 1);
      }
      return;
    }

    if (event.key === 'ArrowLeft' && index > 0) {
      event.preventDefault();
      this.focusInput(index - 1);
      return;
    }

    if (event.key === 'ArrowRight' && index < CODE_LENGTH - 1) {
      event.preventDefault();
      this.focusInput(index + 1);
    }
  }

  protected handlePaste(event: ClipboardEvent): void {
    const text = event.clipboardData?.getData('text') ?? '';
    const digits = text.replace(/\D/g, '').slice(0, CODE_LENGTH);

    if (!digits) {
      return;
    }

    event.preventDefault();

    const inputs = this.digitInputs();
    for (let i = 0; i < CODE_LENGTH; i += 1) {
      const value = digits[i] ?? '';
      this.form.at(i).setValue(value);
      const el = inputs[i]?.nativeElement;
      if (el) {
        el.value = value;
      }
    }

    const nextEmpty = digits.length < CODE_LENGTH ? digits.length : CODE_LENGTH - 1;
    this.focusInput(nextEmpty);
    this.maybeSubmit();
  }

  protected onResend(): void {
    if (this.isSubmitting()) {
      return;
    }
    this.resend.emit();
  }

  protected submitFromButton(): void {
    if (!this.canSubmit()) {
      this.form.markAllAsTouched();
      return;
    }
    this.confirm.emit(this.currentCode());
  }

  private maybeSubmit(): void {
    if (this.canSubmit()) {
      this.confirm.emit(this.currentCode());
    }
  }

  private canSubmit(): boolean {
    if (this.isSubmitting() || this.form.invalid) {
      return false;
    }
    return ALL_DIGITS_PATTERN.test(this.currentCode());
  }

  private currentCode(): string {
    return this.form.controls.map(c => c.value).join('');
  }

  private focusInput(index: number): void {
    const target = this.digitInputs()[index]?.nativeElement;
    target?.focus();
    target?.select();
  }
}

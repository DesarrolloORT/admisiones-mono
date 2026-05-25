import { AsyncPipe } from '@angular/common';
import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  viewChild,
} from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
  OrtSelectModule,
} from '@desarrolloort/components';

import { AuthForm } from '../../components/auth-form/auth-form';
import { LoginFacade } from '../../facades/login.facade';

@Component({
  selector: 'app-login',
  imports: [
    AsyncPipe,
    AuthForm,
    OrtFormFieldModule,
    OrtInputModule,
    OrtSelectModule,
    OrtButtonModule,
    OrtIconModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  providers: [LoginFacade],
  templateUrl: './login.html',
  styleUrl: './login.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Login {
  protected readonly facade = inject(LoginFacade);
  private readonly passwordInput = viewChild<ElementRef<HTMLInputElement>>('passwordInput');

  constructor() {
    if (this.facade.prefilled()) {
      afterNextRender(() => this.passwordInput()?.nativeElement.focus());
    }
  }
}


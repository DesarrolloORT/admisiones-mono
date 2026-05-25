import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
} from '@desarrolloort/components';

import { AuthForm } from '../../components/auth-form/auth-form';
import { SetPasswordFacade } from '../../facades/set-password.facade';

@Component({
  selector: 'app-set-password',
  imports: [
    AuthForm,
    OrtFormFieldModule,
    OrtInputModule,
    OrtButtonModule,
    OrtIconModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  providers: [SetPasswordFacade],
  templateUrl: './set-password.html',
  styleUrl: './set-password.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SetPassword {
  protected readonly facade = inject(SetPasswordFacade);
}


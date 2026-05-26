import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtIconModule,
  OrtInputModule,
} from '@desarrolloort/components';

import { ChangePasswordFacade } from './change-password.facade';

@Component({
  selector: 'app-change-password',
  imports: [
    OrtFormFieldModule,
    OrtInputModule,
    OrtButtonModule,
    OrtIconModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  providers: [ChangePasswordFacade],
  templateUrl: './change-password.html',
  styleUrl: './change-password.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePassword {
  protected readonly facade = inject(ChangePasswordFacade);
}

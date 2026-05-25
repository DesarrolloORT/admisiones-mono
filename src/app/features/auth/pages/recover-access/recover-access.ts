import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  OrtButtonModule,
  OrtFormFieldModule,
  OrtInputModule,
  OrtSelectModule,
} from '@desarrolloort/components';

import { AuthForm } from '../../components/auth-form/auth-form';
import { RecoverAccessFacade } from '../../facades/recover-access.facade';

@Component({
  selector: 'app-recover-access',
  imports: [
    AuthForm,
    OrtFormFieldModule,
    OrtInputModule,
    OrtSelectModule,
    OrtButtonModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  providers: [RecoverAccessFacade],
  templateUrl: './recover-access.html',
  styleUrl: './recover-access.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecoverAccess {
  protected readonly facade = inject(RecoverAccessFacade);
}


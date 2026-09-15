import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { OrtAlertModule } from '@desarrolloort/components';

export interface ErrorAlertState {
  title: string;
  message: string;
}

export const DEFAULT_ERROR_ALERT: ErrorAlertState = {
  title: 'Información incompleta',
  message: 'Revisá y completá los campos obligatorios para continuar.',
};

@Component({
  selector: 'app-error-alert',
  imports: [OrtAlertModule],
  templateUrl: './error-alert.html',
  styleUrl: './error-alert.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorAlert {
  public readonly title = input(DEFAULT_ERROR_ALERT.title);
  public readonly message = input(DEFAULT_ERROR_ALERT.message);
}

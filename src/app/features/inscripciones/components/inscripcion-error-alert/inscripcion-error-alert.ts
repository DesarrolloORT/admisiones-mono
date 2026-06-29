import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { OrtAlertModule } from '@desarrolloort/components';

@Component({
  selector: 'app-inscripcion-error-alert',
  imports: [OrtAlertModule],
  templateUrl: './inscripcion-error-alert.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionErrorAlert {
  public readonly title = input('Información incompleta');
  public readonly message = input.required<string>();
}

import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { OrtButtonModule, OrtIconModule } from '@desarrolloort/components';

@Component({
  selector: 'app-terms-and-conditions',
  imports: [OrtButtonModule, OrtIconModule],
  templateUrl: './terms-and-conditions.html',
  styleUrl: './terms-and-conditions.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TermsAndConditions {
  readonly accepted = output<void>();

  protected onAcceptConditions(): void {
    console.log('Aceptó condiciones');
    this.accepted.emit();
  }
}

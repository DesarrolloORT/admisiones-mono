import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { OrtButtonModule } from '@desarrolloort/components';

@Component({
  selector: 'app-scholarship-granted-card',
  imports: [OrtButtonModule],
  templateUrl: './scholarship-granted-card.html',
  styleUrl: './scholarship-granted-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipGrantedCard {
  readonly openDialog = output<void>();

  onOpenDialogClick() {
    this.openDialog.emit();
  }
}

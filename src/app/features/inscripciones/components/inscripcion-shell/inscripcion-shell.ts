import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { OrtIconModule, OrtStepperModule } from '@desarrolloort/components';

import { PasoInscripcion } from '../../models/inscripcion-flow';

@Component({
  selector: 'app-inscripcion-shell',
  imports: [NgOptimizedImage, OrtIconModule, OrtStepperModule],
  templateUrl: './inscripcion-shell.html',
  styleUrl: './inscripcion-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionShell {
  public readonly stepNumber = input(1);
  public readonly stepLabel = input('');
  public readonly showBack = input(false);
  public readonly showStepper = input(true);
  public readonly steps = input<PasoInscripcion[]>([]);
  public readonly back = output<void>();
  public readonly closeFlow = output<void>();

  protected backClick(): void {
    this.back.emit();
  }
}

import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtIconModule } from '@desarrolloort/components';

@Component({
  selector: 'app-inscripcion-shell',
  imports: [NgOptimizedImage, OrtIconModule, RouterLink],
  templateUrl: './inscripcion-shell.html',
  styleUrl: './inscripcion-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InscripcionShell {
  public readonly stepNumber = input(1);
  public readonly stepLabel = input('');
  public readonly showBack = input(false);
  public readonly showStepper = input(true);
  public readonly back = output<void>();

  protected readonly progressSteps = [1, 2, 3] as const;

  protected backClick(): void {
    this.back.emit();
  }
}

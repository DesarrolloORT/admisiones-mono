import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import {
  ExpandableStepperStep,
  OrtExpandableStepperModule,
  OrtIconModule,
} from '@desarrolloort/components';

@Component({
  selector: 'app-process-layout',
  imports: [NgOptimizedImage, OrtExpandableStepperModule, OrtIconModule],
  templateUrl: './process-layout.html',
  styleUrl: './process-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProcessLayout {
  public readonly brandName = input('Admisiones');
  public readonly closeLabel = input('Cerrar proceso');
  public readonly closeVisible = input(true);
  public readonly currentStepId = input<string | undefined>(undefined);
  public readonly logoAlt = input('ORT');
  public readonly logoSrc = input('/assets/auth/ort-logo-white.svg');
  public readonly processTitle = input('');
  public readonly showBack = input(false);
  public readonly showStepper = input(true);
  public readonly stepperAriaLabel = input('Pasos del proceso');
  public readonly stepperExpanded = input(false);
  public readonly stepperSubtitle = input<string | undefined>(undefined);
  public readonly steps = input<ExpandableStepperStep[]>([]);

  public readonly back = output<void>();
  public readonly closeFlow = output<void>();

  protected backClick(): void {
    this.back.emit();
  }
}

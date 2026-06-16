import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { ExpandableStepperStep, ExpandableStepperStepStatus } from '@desarrolloort/components';
import { ProcessLayout } from 'src/app/shared/ui/process-layout/process-layout';

import { PasoInscripcion } from '../../models/inscripcion-flow';

@Component({
  selector: 'app-inscripcion-shell',
  imports: [ProcessLayout],
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

  protected readonly currentStepId = computed(() => {
    const steps = this.steps();
    return steps.find(step => step.status === 'actual')?.id ?? steps[this.stepNumber() - 1]?.id;
  });
  protected readonly processSteps = computed<ExpandableStepperStep[]>(() =>
    this.steps().map(step => ({
      id: step.id,
      overline: step.overline,
      status: this.toExpandableStepperStatus(step.status),
      title: step.title,
    }))
  );

  protected backClick(): void {
    this.back.emit();
  }

  private toExpandableStepperStatus(
    status: PasoInscripcion['status']
  ): ExpandableStepperStepStatus {
    if (status === 'completo') return 'completed';
    if (status === 'actual') return 'current';
    return 'pending';
  }
}

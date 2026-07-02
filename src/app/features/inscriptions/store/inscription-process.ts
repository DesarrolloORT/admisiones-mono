import { signal } from '@angular/core';
import { createProcessFlow } from 'src/app/shared/process-flow/process-flow';

import type { InscripcionPreEnrollmentResponse } from '../models/inscription-flow';
import { INSCRIPCION_STEPS } from '../models/inscription-process';

export class InscripcionProcessStore {
  public readonly flow = createProcessFlow(INSCRIPCION_STEPS, 'propuesta');
  public readonly preEnrollmentResponse = signal<InscripcionPreEnrollmentResponse | null>(null);
  public readonly checkpoint = signal(0);

  public markCheckpoint(): void {
    this.checkpoint.update(value => value + 1);
  }
}

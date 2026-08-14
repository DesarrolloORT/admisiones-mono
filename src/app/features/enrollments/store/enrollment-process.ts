import { signal } from '@angular/core';
import { createProcessFlow } from 'src/app/shared/process-flow/process-flow';

import type { EnrollmentPreEnrollmentResponse } from '../models/enrollment-flow';
import { ENROLLMENT_STEPS } from '../models/enrollment-process';

export class EnrollmentProcessStore {
  public readonly flow = createProcessFlow(ENROLLMENT_STEPS, 'proposal');
  public readonly preEnrollmentResponse = signal<EnrollmentPreEnrollmentResponse | null>(null);
}

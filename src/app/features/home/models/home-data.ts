import { EnrollmentSummary } from './enrollment-summary';
import { ScholarshipSummary } from './scholarship-summary';

export interface HomeData {
  enrollments: EnrollmentSummary[];
  scholarships: ScholarshipSummary[];
}

export const DEFAULT_HOME_LOAD_ERROR = 'Hubo un error al cargar tu información.';

export interface HomeLoadFailure {
  loadError: string;
}

export type HomeResolved = HomeData | HomeLoadFailure;

export function isHomeLoadFailure(resolved: HomeResolved): resolved is HomeLoadFailure {
  return 'loadError' in resolved;
}

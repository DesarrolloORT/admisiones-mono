import { EnrollmentSummary } from './enrollment-summary';
import { ScholarshipSummary } from './scholarship-summary';

export interface HomeData {
  enrollments: EnrollmentSummary[];
  scholarships: ScholarshipSummary[];
}

import {
  COMPLETE_SURVEY_SECTIONS,
  EnrollmentScenario,
  PaymentResult,
  PROFESSIONAL_UPDATE_SURVEY_SECTIONS,
  SURVEY_SECTIONS,
  SurveySectionId,
} from './enrollment-flow';

export function parseForcedResult(value: string | null): PaymentResult | null {
  return value === 'en-proceso' ? 'in-progress' : null;
}

export function getVisibleSections(
  scenario: EnrollmentScenario,
  isProfessionalUpdate = false
): readonly SurveySectionId[] {
  if (isProfessionalUpdate) return PROFESSIONAL_UPDATE_SURVEY_SECTIONS;

  const base = scenario === 'survey-complete' ? COMPLETE_SURVEY_SECTIONS : SURVEY_SECTIONS;
  return base;
}

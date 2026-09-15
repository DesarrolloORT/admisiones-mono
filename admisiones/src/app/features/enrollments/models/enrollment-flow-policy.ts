import {
  COMPLETE_SURVEY_SECTIONS,
  PaymentResult,
  PROFESSIONAL_UPDATE_SURVEY_SECTIONS,
  SURVEY_SECTIONS,
  SurveySectionId,
} from './enrollment-flow';

export function parseForcedResult(value: string | null): PaymentResult | null {
  return value === 'en-proceso' ? 'in-progress' : null;
}

export function getVisibleSections(
  canAnswerSurvey: boolean,
  isProfessionalUpdate = false
): readonly SurveySectionId[] {
  if (isProfessionalUpdate) return PROFESSIONAL_UPDATE_SURVEY_SECTIONS;

  return canAnswerSurvey ? SURVEY_SECTIONS : COMPLETE_SURVEY_SECTIONS;
}

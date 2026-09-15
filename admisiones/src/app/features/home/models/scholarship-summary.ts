export type ScholarshipStatus = 'En proceso' | 'Consulta' | 'Aceptada' | 'Estudio' | string;

export interface ScholarshipSummary {
  id: number;
  scholarshipName: string;
  degreeProgramName: string;
  status: ScholarshipStatus;
  applicationDeadline: string;
  examDate: string;
  examResult: string;
  benefit: string;
  resultsDate: string;
}

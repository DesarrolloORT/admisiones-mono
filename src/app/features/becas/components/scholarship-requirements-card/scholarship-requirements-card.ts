import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrtIconModule } from '@desarrolloort/components';

export type ScholarshipType = 'revalidation' | 'academic' | 'socioeconomic';

interface ScholarshipRequirement {
  title: string;
  description: string;
}

interface ScholarshipConfig {
  requirements: ScholarshipRequirement[];
}

export const SCHOLARSHIP_REQUIREMENTS_CONFIG: Record<ScholarshipType, ScholarshipConfig> = {
  revalidation: {
    requirements: [
      {
        title: 'Perfil del estudiante',
        description:
          'Dirigido a quienes revalidan materias de otras universidades (nacionales o extranjeras).',
      },
      {
        title: 'Antecedentes académicos',
        description:
          'Deberás detallar materias aprobadas, reprobadas y promedios de la institución de origen.',
      },
      {
        title: 'Documentación',
        description:
          'Es necesario adjuntar el formulario de reválidas firmado por la Coordinación Académica de Universidad ORT.',
      },
    ],
  },

  academic: {
    requirements: [
      {
        title: 'Promedio académico',
        description: 'Dirigido a estudiantes con alto rendimiento académico comprobable.',
      },
      {
        title: 'Regularidad',
        description: 'El estudiante deberá mantener la regularidad en la carrera.',
      },
    ],
  },

  socioeconomic: {
    requirements: [
      {
        title: 'Situación económica',
        description: 'Dirigido a estudiantes que acrediten necesidad de apoyo económico.',
      },
      {
        title: 'Documentación respaldatoria',
        description:
          'Se deberá presentar documentación que permita evaluar la situación socioeconómica.',
      },
    ],
  },
};

@Component({
  selector: 'app-scholarship-requirements-card',
  imports: [OrtIconModule],
  templateUrl: './scholarship-requirements-card.html',
  styleUrl: './scholarship-requirements-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipRequirementsCard {
  readonly variant = input.required<ScholarshipType>();

  protected readonly config = computed<ScholarshipConfig>(
    () => SCHOLARSHIP_REQUIREMENTS_CONFIG[this.variant()]
  );
}

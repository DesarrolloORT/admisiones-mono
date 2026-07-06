import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { OrtIconModule } from '@desarrolloort/components';

import { ScholarshipVariant } from '../../models/scholarship-personal-forms';

interface ScholarshipRequirement {
  title: string;
  description: string;
}

interface ScholarshipConfig {
  requirements: ScholarshipRequirement[];
  methods: ScholarshipRequirement[];
}

export const SCHOLARSHIP_REQUIREMENTS_CONFIG: Record<ScholarshipVariant, ScholarshipConfig> = {
  fbr: {
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
    methods: [],
  },
  fexaCon: {
    requirements: [
      {
        title: 'Calificación mínima exigida',
        description:
          '8 o más en 2.° de EMS, o 7 en 2.º de EMS y 8 o más en 3.º de EMS, o Bachillerato Internacional (IB) aprobado.',
      },
      {
        title: 'Cursado de 3.º de EMS',
        description:
          'Haber cursado 3.° de EMS en el año lectivo inmediato anterior al inicio de la carrera.',
      },
      {
        title: 'Certificado de secundaria',
        description: 'Es necesario presentar la Fórmula 69 para validar tus notas.',
      },
      {
        title: 'Prueba de aptitud académica',
        description: 'Deberás seleccionar una fecha para rendir la prueba.',
      },
    ],
    methods: [
      {
        title: 'Con declaración jurada',
        description:
          'otorga hasta 60% de beca. Deberás completar información sobre ingresos y gastos de tu núcleo familiar.',
      },
      {
        title: 'Sin declaración jurada',
        description: 'otorga hasta un 35% de beca. No requiere información económica.',
      },
    ],
  },
  fexaSin: {
    requirements: [
      {
        title: 'Calificación mínima exigida',
        description:
          '8 o más en 2.° de EMS, o 7 en 2.º de EMS y 8 o más en 3.º de EMS, o Bachillerato Internacional (IB) aprobado.',
      },
      {
        title: 'Cursado de 3.º de EMS',
        description:
          'Haber cursado 3.° de EMS en el año lectivo inmediato anterior al inicio de la carrera.',
      },
      {
        title: 'Certificado de secundaria',
        description: 'Es necesario presentar la Fórmula 69 para validar tus notas.',
      },
      {
        title: 'Prueba de aptitud académica',
        description: 'Deberás seleccionar una fecha para rendir la prueba.',
      },
    ],
    methods: [
      {
        title: 'Con declaración jurada',
        description:
          'otorga hasta 60% de beca. Deberás completar información sobre ingresos y gastos de tu núcleo familiar.',
      },
      {
        title: 'Sin declaración jurada',
        description: 'otorga hasta un 35% de beca. No requiere información económica.',
      },
    ],
  },
  fbc: {
    requirements: [
      {
        title: 'Perfil del estudiante',
        description:
          'Ingreso desde cero con bachillerato completo o con materias de 3.° de EMS pendientes.',
      },
      {
        title: 'Certificado de secundaria',
        description: 'Es necesario presentar la Fórmula 69 para validar tus notas.',
      },
      {
        title: 'Prueba de aptitud académica',
        description: 'Deberás seleccionar una fecha para rendir la prueba.',
      },
    ],
    methods: [],
  },
  fcl: {
    requirements: [
      {
        title: 'Perfil del estudiante',
        description: 'Dirigido a programas de nivel corto o capacitación laboral.',
      },
      {
        title: 'Requisitos académicos',
        description: 'Mínimo 1.° de EMS (4.° año) aprobado.',
      },
    ],
    methods: [],
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
  readonly variant = input.required<ScholarshipVariant>();

  protected readonly config = computed<ScholarshipConfig>(
    () => SCHOLARSHIP_REQUIREMENTS_CONFIG[this.variant()]
  );
}

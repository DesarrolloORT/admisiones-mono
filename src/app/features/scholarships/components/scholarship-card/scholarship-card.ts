import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrtButtonModule, OrtChipModule } from '@desarrolloort/components';

/**
 * Lo que la card necesita para pintarse. `title`, `description` y `requiresExam`
 * vienen de `GET /scholarships/available`; `route` y `requiresEnrollment` los
 * pone el front desde `models/scholarship-catalog.ts`.
 */
export interface ScholarshipCardModel {
  title: string;
  description: string;
  requiresExam: boolean;
  /** Si la beca exige tener la inscripcion paga antes de poder postularse. */
  requiresEnrollment: boolean;
  /** `null` cuando el front todavia no tiene pantalla para esa beca. */
  route: string | null;
}

@Component({
  selector: 'app-scholarship-card',
  imports: [OrtButtonModule, OrtChipModule, RouterLink],
  templateUrl: './scholarship-card.html',
  styleUrl: './scholarship-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarshipCard {
  readonly scholarship = input.required<ScholarshipCardModel>();
  readonly isEnrolled = input.required<boolean>();
  readonly single = input<boolean>(false);

  /**
   * Cada card necesita un `id` propio: con uno fijo, todos los `aria-labelledby`
   * de la pantalla apuntan al titulo de la primera.
   */
  protected readonly titleId = computed(() => {
    const slug = this.scholarship()
      .title.normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9]+/gi, '-')
      .toLowerCase();

    return `scholarship-card-title-${slug}`;
  });
}

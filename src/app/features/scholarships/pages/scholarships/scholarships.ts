import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { catchError, of } from 'rxjs';
import { HomeHeader } from 'src/app/shared/ui/home-header/home-header';

import { ScholarshipsApi } from '../../api/scholarships.api';
import type { ScholarshipCardModel } from '../../components/scholarship-card/scholarship-card';
import { ScholarshipCard } from '../../components/scholarship-card/scholarship-card';
import type { AvailableScholarships } from '../../models/available-scholarship.interface';
import { resolveScholarshipCatalogEntry } from '../../models/scholarship-catalog';

/**
 * Estado inicial y de error: el caso restrictivo. Mientras la respuesta no
 * llegue (o si falla) no se ofrece postularse a nada.
 */
const EMPTY_CATALOG: AvailableScholarships = {
  requiresPriorEnrollment: true,
  scholarships: [],
};

@Component({
  selector: 'app-scholarships',
  imports: [ScholarshipCard, HomeHeader],
  templateUrl: './scholarships.html',
  styleUrl: './scholarships.scss',

  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Scholarships {
  private readonly scholarshipsApi = inject(ScholarshipsApi);
  private readonly breakpointService = inject(BreakpointService);

  /**
   * La page inyecta **el adapter de su feature** — no hay service de reenvio ni
   * store. `toSignal` convierte el Observable en signal para que el template lo
   * lea con `@for`; el `catchError` evita que un backend caido deje la pantalla
   * en blanco.
   */
  private readonly available = toSignal(
    this.scholarshipsApi.getAvailableScholarships().pipe(catchError(() => of(EMPTY_CATALOG))),
    { initialValue: EMPTY_CATALOG }
  );

  /** `requiresPriorEnrollment` es la respuesta del backend a "¿puede postularse?". */
  protected readonly isEnrolled = computed(() => !this.available().requiresPriorEnrollment);

  /**
   * El unico lugar donde se une lo que dice la API (textos) con lo que sabe el
   * front (a donde lleva la card y si exige inscripcion previa).
   */
  protected readonly cards = computed<ScholarshipCardModel[]>(() =>
    this.available().scholarships.map(scholarship => {
      const entry = resolveScholarshipCatalogEntry(
        scholarship.scholarshipTypeIds,
        scholarship.name
      );

      return {
        title: scholarship.name,
        description: scholarship.description,
        requiresExam: scholarship.requiresTest,
        requiresEnrollment: entry?.requiresEnrollment ?? true,
        route: entry?.route ?? null,
      };
    })
  );

  protected readonly withoutEnrollment = computed(() =>
    this.cards().filter(card => !card.requiresEnrollment)
  );

  protected readonly withEnrollment = computed(() =>
    this.cards().filter(card => card.requiresEnrollment)
  );

  protected readonly hasScholarships = computed(() => this.cards().length > 0);

  readonly showBack = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall;
  });
}

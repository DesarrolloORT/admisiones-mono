import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { BreakpointService } from '@desarrolloort/ngx-utils';
import { catchError, of } from 'rxjs';
import { AuthSessionService } from 'src/app/features/auth/services/auth-session';
import { HomeHeader } from 'src/app/shared/ui/home-header/home-header';

import { ScholarshipsApi } from '../../api/scholarships.api';
import type { ScholarshipCardModel } from '../../components/scholarship-card/scholarship-card';
import { ScholarshipCard } from '../../components/scholarship-card/scholarship-card';
import type { AvailableScholarships } from '../../models/available-scholarship.interface';
import type { ScholarshipKind } from '../../models/scholarship-catalog';
import { resolveScholarshipCatalogEntry } from '../../models/scholarship-catalog';

/**
 * Estado inicial y de error: el caso restrictivo. Mientras la respuesta no
 * llegue (o si falla) no se ofrece postularse a nada.
 */
const EMPTY_CATALOG: AvailableScholarships = {
  requiresPriorEnrollment: true,
  scholarships: [],
};

/** Orden fijo de exhibición, pedido por negocio; no depende del orden del backend. */
const DISPLAY_ORDER: readonly ScholarshipKind[] = ['fexa', 'fbc', 'fcl', 'fbr'];

/** Las becas que el front todavía no reconoce quedan al final. */
function displayRank(kind: ScholarshipKind | undefined): number {
  const index = kind ? DISPLAY_ORDER.indexOf(kind) : -1;

  return index === -1 ? DISPLAY_ORDER.length : index;
}

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
  private readonly authSession = inject(AuthSessionService);

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
    this.available()
      .scholarships.map(scholarship => ({
        scholarship,
        entry: resolveScholarshipCatalogEntry(scholarship.scholarshipTypeIds, scholarship.name),
      }))
      .sort((a, b) => displayRank(a.entry?.kind) - displayRank(b.entry?.kind))
      .map(({ scholarship, entry }) => ({
        title: scholarship.name,
        description: scholarship.description,
        requiresExam: scholarship.requiresTest,
        requiresEnrollment: entry?.requiresEnrollment ?? true,
        route: entry?.route ?? null,
      }))
  );

  protected readonly hasScholarships = computed(() => this.cards().length > 0);

  readonly showBack = computed(() => {
    const breakpoint = this.breakpointService.breakpoint();

    return breakpoint.isXSmall || breakpoint.isSmall;
  });

  protected logout(): void {
    this.authSession.logout();
  }
}

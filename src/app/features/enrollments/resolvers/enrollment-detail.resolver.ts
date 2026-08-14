import { inject } from '@angular/core';
import { ParamMap, ResolveFn } from '@angular/router';
import { forkJoin, Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import {
  ACADEMIC_PROPOSAL_TYPE_IDS,
  getAcademicProposalTypeByLevel,
} from '../../catalogs/models/academic-proposal';
import type { DegreeProgram } from '../../catalogs/models/catalog.interface';
import { Catalogs } from '../../catalogs/services/catalogs';
import type { EnrollmentDetail } from '../models/enrollment-detail';
import type { EnrollmentEntryResolved } from '../models/enrollment-entry';
import { EnrollmentResumeContextStore } from '../services/enrollment-resume-context';
import { Enrollments } from '../services/enrollments';

/**
 * Lo que la URL pide, ya validado: empezar de cero, o entrar a UNA inscripción
 * concreta (producto+proceso la identifican; `modo=reactivar` la distingue de retomar).
 */
type EntryRequest =
  | { intent: 'new' }
  | {
      intent: 'resume' | 'reactivate';
      productId: number;
      admissionProcessId: number;
      offeringIds: number[];
      status: string | null;
      productLevelId: number | null;
    };

export const enrollmentDetailResolver: ResolveFn<EnrollmentEntryResolved> = route => {
  const request = readEntryRequest(route.queryParamMap);

  // Sin inscripción identificada no hay nada que cargar: el flujo arranca virgen.
  if (request.intent === 'new') return of({ intent: 'new' });

  const { intent, productId, admissionProcessId } = request;
  const resumeContext = inject(EnrollmentResumeContextStore);

  // 1. Ofertas: manda la URL (enlaces anteriores); si no viene, las del panel en sessionStorage.
  const offeringIds = request.offeringIds.length
    ? request.offeringIds
    : (resumeContext.read(productId, admissionProcessId)?.offeringIds ?? []);

  // 2. Reactivar ya trae la inscripción que creó el POST; se consume una sola vez.
  const preEnrollment =
    intent === 'reactivate' ? resumeContext.takeReactivation(productId, admissionProcessId) : null;

  // 3. Las dos cargas son opcionales: degradan a null/[] para que el flujo abra igual.
  const detail$ = preEnrollment
    ? of(null)
    : loadDetail(productId, admissionProcessId, request.status);
  // El panel ya mandó el nivel por `nivel`; solo cuando falta hay que reconstruirlo, y eso
  // cuesta una consulta por tipo de propuesta.
  const careers$ = request.productLevelId === null ? loadCareers() : of<DegreeProgram[]>([]);

  return forkJoin({ detail: detail$, careers: careers$ }).pipe(
    map(({ detail, careers }) => {
      const entry = {
        detail,
        productId,
        admissionProcessId,
        offeringIds,
        productLevelId:
          request.productLevelId ??
          findProductLevel(careers, detail?.summary?.productId ?? productId),
      };

      return intent === 'reactivate' ? { ...entry, intent, preEnrollment } : { ...entry, intent };
    })
  );
};

/**
 * Traduce los query params a la intención de entrada. Pura y decidida solo con la URL:
 * nunca se infiere del backend (sin eso, una encuesta en progreso precargaba el paso 1).
 */
export function readEntryRequest(params: ParamMap): EntryRequest {
  const productId = toPositiveInteger(params.get('idProducto'));
  const admissionProcessId = toPositiveInteger(params.get('idProceso'));
  if (productId === null || admissionProcessId === null) return { intent: 'new' };

  return {
    intent: params.get('modo') === 'reactivar' ? 'reactivate' : 'resume',
    productId,
    admissionProcessId,
    offeringIds: toPositiveIntegers(params.getAll('idOferta')),
    status: toNonEmptyString(params.get('estado')),
    productLevelId: toKnownProductLevel(params.get('nivel')),
  };
}

function toKnownProductLevel(value: string | null): number | null {
  const level = toPositiveInteger(value);
  return level !== null && getAcademicProposalTypeByLevel(level) ? level : null;
}

export const resolveEntryIntent = (params: ParamMap): EnrollmentEntryResolved['intent'] =>
  readEntryRequest(params).intent;

/** El Detalle es informativo: si falla, el paso 2 sigue con lo que aporta la URL. */
function loadDetail(
  productId: number,
  admissionProcessId: number,
  status: string | null
): Observable<EnrollmentDetail | null> {
  return inject(Enrollments)
    .getDetail(productId, admissionProcessId, status)
    .pipe(catchError(() => of(null)));
}

/** Carreras de todas las propuestas, solo para deducir el nivel del producto. */
function loadCareers(): Observable<DegreeProgram[]> {
  const catalogs = inject(Catalogs);
  return forkJoin(ACADEMIC_PROPOSAL_TYPE_IDS.map(type => catalogs.getDegreePrograms(type))).pipe(
    map(groups => groups.flat()),
    catchError(() => of([]))
  );
}

/** El Detalle es la fuente preferida del producto; con el Detalle caído, el param. */
function findProductLevel(careers: DegreeProgram[], productId: number): number | null {
  return careers.find(career => career.productId === productId)?.productLevelId ?? null;
}

function toPositiveInteger(value: string | null): number | null {
  if (!value || !/^\d+$/.test(value)) return null;
  const parsed = Number(value);
  return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : null;
}

function toPositiveIntegers(values: string[]): number[] {
  return [...new Set(values.map(toPositiveInteger).filter((id): id is number => id !== null))];
}

function toNonEmptyString(value: string | null): string | null {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}

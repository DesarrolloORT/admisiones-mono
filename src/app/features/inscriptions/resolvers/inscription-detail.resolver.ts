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
import type { InscripcionDetail } from '../models/inscription-detail';
import type { InscripcionEntryResolved } from '../models/inscription-entry';
import { InscriptionResumeContextStore } from '../services/inscription-resume-context';
import { Inscripciones } from '../services/inscriptions';

/**
 * Lo que la URL pide, ya validado: empezar de cero, o entrar a UNA inscripción
 * concreta (producto+proceso la identifican; `modo=reactivar` la distingue de retomar).
 */
type EntryRequest =
  | { intent: 'nueva' }
  | {
      intent: 'retomar' | 'reactivar';
      idProducto: number;
      idProceso: number;
      idOfertas: number[];
      estado: string | null;
      idNivelProducto: number | null;
    };

export const inscriptionDetailResolver: ResolveFn<InscripcionEntryResolved> = route => {
  const request = readEntryRequest(route.queryParamMap);

  // Sin inscripción identificada no hay nada que cargar: el flujo arranca virgen.
  if (request.intent === 'nueva') return of({ intent: 'nueva' });

  const { intent, idProducto, idProceso } = request;
  const resumeContext = inject(InscriptionResumeContextStore);

  // 1. Ofertas: manda la URL (enlaces anteriores); si no viene, las del panel en sessionStorage.
  const idOfertas = request.idOfertas.length
    ? request.idOfertas
    : (resumeContext.read(idProducto, idProceso)?.idOfertas ?? []);

  // 2. Reactivar ya trae la inscripción que creó el POST; se consume una sola vez.
  const preEnrollment =
    intent === 'reactivar' ? resumeContext.takeReactivation(idProducto, idProceso) : null;

  // 3. Las dos cargas son opcionales: degradan a null/[] para que el flujo abra igual.
  const detail$ = preEnrollment ? of(null) : loadDetail(idProducto, idProceso, request.estado);
  // El panel ya mandó el nivel por `nivel`; solo cuando falta hay que reconstruirlo, y eso
  // cuesta una consulta por tipo de propuesta.
  const careers$ = request.idNivelProducto === null ? loadCareers() : of<DegreeProgram[]>([]);

  return forkJoin({ detail: detail$, careers: careers$ }).pipe(
    map(({ detail, careers }) => {
      const entry = {
        detail,
        idProducto,
        idProceso,
        idOfertas,
        idNivelProducto:
          request.idNivelProducto ??
          findProductLevel(careers, detail?.detalle?.idProducto ?? idProducto),
      };

      return intent === 'reactivar' ? { ...entry, intent, preEnrollment } : { ...entry, intent };
    })
  );
};

/**
 * Traduce los query params a la intención de entrada. Pura y decidida solo con la URL:
 * nunca se infiere del backend (sin eso, una encuesta en progreso precargaba el paso 1).
 */
export function readEntryRequest(params: ParamMap): EntryRequest {
  const idProducto = toPositiveInteger(params.get('idProducto'));
  const idProceso = toPositiveInteger(params.get('idProceso'));
  if (idProducto === null || idProceso === null) return { intent: 'nueva' };

  return {
    intent: params.get('modo') === 'reactivar' ? 'reactivar' : 'retomar',
    idProducto,
    idProceso,
    idOfertas: toPositiveIntegers(params.getAll('idOferta')),
    estado: toNonEmptyString(params.get('estado')),
    idNivelProducto: toKnownProductLevel(params.get('nivel')),
  };
}

function toKnownProductLevel(value: string | null): number | null {
  const level = toPositiveInteger(value);
  return level !== null && getAcademicProposalTypeByLevel(level) ? level : null;
}

export const resolveEntryIntent = (params: ParamMap): InscripcionEntryResolved['intent'] =>
  readEntryRequest(params).intent;

/** El Detalle es informativo: si falla, el paso 2 sigue con lo que aporta la URL. */
function loadDetail(
  idProducto: number,
  idProceso: number,
  estado: string | null
): Observable<InscripcionDetail | null> {
  return inject(Inscripciones)
    .getDetail(idProducto, idProceso, estado)
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
function findProductLevel(careers: DegreeProgram[], idProducto: number): number | null {
  return careers.find(career => career.productId === idProducto)?.productLevelId ?? null;
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

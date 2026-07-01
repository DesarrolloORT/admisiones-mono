import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { storageKeys } from 'src/app/core/storage/keys';

import {
  type BorradorInscripcion,
  type EscenarioInscripcion,
  SECCIONES_ENCUESTA,
} from '../models/inscripcion-flow';

const PASOS_PERSISTIBLES: readonly BorradorInscripcion['paso'][] = [
  'propuesta',
  'encuesta',
  'pago',
];

@Injectable({
  providedIn: 'root',
})
export class InscripcionDraft {
  private readonly document = inject(DOCUMENT);

  public load(scenario: EscenarioInscripcion): BorradorInscripcion | null {
    const key = this.getKey(scenario);
    if (!key) return null;
    const rawDraft = this.storage?.getItem(key);

    if (!rawDraft) {
      return null;
    }

    try {
      const draft = JSON.parse(rawDraft) as Partial<BorradorInscripcion>;

      if (this.isValidDraft(draft, scenario)) {
        return draft;
      }

      this.clear(scenario);
      return null;
    } catch {
      this.clear(scenario);
      return null;
    }
  }

  public save(draft: BorradorInscripcion): void {
    const key = this.getKey(draft.escenario);
    if (!key) return;
    try {
      this.storage?.setItem(key, JSON.stringify(draft));
    } catch {
      // El guardado local es auxiliar; el backend sigue siendo la persistencia autoritativa.
    }
  }

  public clear(scenario: EscenarioInscripcion): void {
    const key = this.getKey(scenario);
    if (!key) return;
    try {
      this.storage?.removeItem(key);
    } catch {
      // sessionStorage puede no estar disponible por políticas del navegador.
    }
  }

  private isValidDraft(
    draft: Partial<BorradorInscripcion>,
    scenario: EscenarioInscripcion
  ): draft is BorradorInscripcion {
    return (
      draft.version === 2 &&
      draft.escenario === scenario &&
      PASOS_PERSISTIBLES.includes(draft.paso as BorradorInscripcion['paso']) &&
      SECCIONES_ENCUESTA.includes(draft.seccionActiva as BorradorInscripcion['seccionActiva']) &&
      Array.isArray(draft.seccionesCompletas) &&
      draft.seccionesCompletas.every(section =>
        SECCIONES_ENCUESTA.includes(section as BorradorInscripcion['seccionActiva'])
      ) &&
      this.isRecord(draft.propuesta) &&
      this.isRecord(draft.encuesta) &&
      this.isRecord(draft.identidad) &&
      this.isRecord(draft.reglamento) &&
      this.isRecord(draft.pago) &&
      (draft.preinscripcion === null || this.isRecord(draft.preinscripcion))
    );
  }

  private isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
  }

  private getKey(scenario: EscenarioInscripcion): string | null {
    const userKey = this.getUserKey();
    return userKey ? `${storageKeys.inscriptionDraft}:${userKey}:${scenario}` : null;
  }

  private getUserKey(): string | null {
    const rawSession = this.document.defaultView?.localStorage.getItem(storageKeys.session);

    if (!rawSession) {
      return null;
    }

    try {
      const session = JSON.parse(rawSession) as { documentNumber?: unknown };
      return typeof session.documentNumber === 'string' && session.documentNumber
        ? session.documentNumber
        : null;
    } catch {
      return null;
    }
  }

  private get storage(): Storage | null {
    return this.document.defaultView?.sessionStorage ?? null;
  }
}

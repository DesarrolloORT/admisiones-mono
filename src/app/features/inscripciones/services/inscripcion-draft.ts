import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { storageKeys } from 'src/app/core/storage/keys';

import {
  type BorradorInscripcion,
  type EscenarioInscripcion,
  SECCIONES_ENCUESTA,
} from '../models/inscripcion-flow';

const PANTALLAS_PERSISTIBLES: readonly BorradorInscripcion['pantalla'][] = [
  'propuesta',
  'encuesta',
  'lector-reglamento',
  'pago',
  'confirmacion-pago',
];

@Injectable({
  providedIn: 'root',
})
export class InscripcionDraft {
  private readonly document = inject(DOCUMENT);

  public load(scenario: EscenarioInscripcion): BorradorInscripcion | null {
    const rawDraft = this.storage?.getItem(this.getKey(scenario));

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
    this.storage?.setItem(this.getKey(draft.escenario), JSON.stringify(draft));
  }

  public clear(scenario: EscenarioInscripcion): void {
    this.storage?.removeItem(this.getKey(scenario));
  }

  private isValidDraft(
    draft: Partial<BorradorInscripcion>,
    scenario: EscenarioInscripcion
  ): draft is BorradorInscripcion {
    return (
      draft.version === 1 &&
      draft.escenario === scenario &&
      PANTALLAS_PERSISTIBLES.includes(draft.pantalla as BorradorInscripcion['pantalla']) &&
      SECCIONES_ENCUESTA.includes(draft.seccionActiva as BorradorInscripcion['seccionActiva']) &&
      Array.isArray(draft.seccionesCompletas) &&
      draft.seccionesCompletas.every(section =>
        SECCIONES_ENCUESTA.includes(section as BorradorInscripcion['seccionActiva'])
      ) &&
      this.isRecord(draft.propuesta) &&
      this.isRecord(draft.encuesta) &&
      this.isRecord(draft.identidad) &&
      this.isRecord(draft.reglamento) &&
      this.isRecord(draft.pago)
    );
  }

  private isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
  }

  private getKey(scenario: EscenarioInscripcion): string {
    return `inscripcion-borrador:v1:${this.getUserKey()}:${scenario}`;
  }

  private getUserKey(): string {
    const rawSession = this.document.defaultView?.localStorage.getItem(storageKeys.session);

    if (!rawSession) {
      return 'anonimo';
    }

    try {
      const session = JSON.parse(rawSession) as { documentNumber?: unknown };
      return typeof session.documentNumber === 'string' && session.documentNumber
        ? session.documentNumber
        : 'anonimo';
    } catch {
      return 'anonimo';
    }
  }

  private get storage(): Storage | null {
    return this.document.defaultView?.sessionStorage ?? null;
  }
}

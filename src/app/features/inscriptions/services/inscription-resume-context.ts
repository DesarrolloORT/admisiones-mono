import { Injectable } from '@angular/core';

const STORAGE_KEY = 'inscription-resume-context';

export interface InscriptionResumeContext {
  idProducto: number;
  idProceso: number;
  idOfertas: number[];
  idInscripciones: number[];
}

@Injectable({ providedIn: 'root' })
export class InscriptionResumeContextStore {
  public save(context: InscriptionResumeContext): void {
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(context));
    } catch {
      // El resolver conserva el fallback de URL/Detalle.
    }
  }

  public read(idProducto: number, idProceso: number): InscriptionResumeContext | null {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (!raw) return null;

      const parsed: unknown = JSON.parse(raw);
      if (!parsed || typeof parsed !== 'object') return null;
      const context = parsed as Partial<InscriptionResumeContext>;
      if (
        !isPositiveInteger(context.idProducto) ||
        !isPositiveInteger(context.idProceso) ||
        !Array.isArray(context.idOfertas) ||
        !Array.isArray(context.idInscripciones) ||
        context.idProducto !== idProducto ||
        context.idProceso !== idProceso
      ) {
        return null;
      }

      return {
        idProducto: context.idProducto,
        idProceso: context.idProceso,
        idOfertas: positiveIds(context.idOfertas),
        idInscripciones: positiveIds(context.idInscripciones),
      };
    } catch {
      return null;
    }
  }

  public clear(): void {
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      // El flujo sigue usando Detalle si el navegador bloquea sessionStorage.
    }
  }
}

function positiveIds(values: readonly unknown[]): number[] {
  return Array.from(new Set(values.filter(isPositiveInteger)));
}

function isPositiveInteger(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value > 0;
}

import { Injectable } from '@angular/core';

import type { EnrollmentPreEnrollmentResponse } from '../models/enrollment-flow';

const STORAGE_KEY = 'inscription-resume-context';

export interface EnrollmentResumeContext {
  productId: number;
  admissionProcessId: number;
  offeringIds: number[];
  enrollmentIds: number[];
}

@Injectable({ providedIn: 'root' })
export class EnrollmentResumeContextStore {
  private reactivation: {
    productId: number;
    admissionProcessId: number;
    response: EnrollmentPreEnrollmentResponse;
  } | null = null;

  public save(context: EnrollmentResumeContext): void {
    try {
      sessionStorage.setItem(
        STORAGE_KEY,
        JSON.stringify({
          idProducto: context.productId,
          idProceso: context.admissionProcessId,
          idOfertas: context.offeringIds,
          idEnrollments: context.enrollmentIds,
        })
      );
    } catch {
      // El resolver conserva el fallback de URL/Detalle.
    }
  }

  public read(
    requestedProductId: number,
    requestedAdmissionProcessId: number
  ): EnrollmentResumeContext | null {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (!raw) return null;

      const parsed: unknown = JSON.parse(raw);
      if (!parsed || typeof parsed !== 'object') return null;
      const stored = parsed as Record<string, unknown>;
      const productId = stored['idProducto'];
      const admissionProcessId = stored['idProceso'];
      const offeringIds = stored['idOfertas'];
      const enrollmentIds = stored['idEnrollments'];
      if (
        !isPositiveInteger(productId) ||
        !isPositiveInteger(admissionProcessId) ||
        !Array.isArray(offeringIds) ||
        !Array.isArray(enrollmentIds) ||
        productId !== requestedProductId ||
        admissionProcessId !== requestedAdmissionProcessId
      ) {
        return null;
      }

      return {
        productId,
        admissionProcessId,
        offeringIds: positiveIds(offeringIds),
        enrollmentIds: positiveIds(enrollmentIds),
      };
    } catch {
      return null;
    }
  }

  public saveReactivation(
    context: EnrollmentResumeContext,
    response: EnrollmentPreEnrollmentResponse
  ): void {
    this.save(context);
    this.reactivation = {
      productId: context.productId,
      admissionProcessId: context.admissionProcessId,
      response,
    };
  }

  public takeReactivation(
    productId: number,
    admissionProcessId: number
  ): EnrollmentPreEnrollmentResponse | null {
    if (
      this.reactivation?.productId !== productId ||
      this.reactivation.admissionProcessId !== admissionProcessId
    ) {
      return null;
    }

    const response = this.reactivation.response;
    this.reactivation = null;
    return response;
  }

  public clear(): void {
    this.reactivation = null;
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

import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import {
  getInscripcionesEncuestaInicialEndpoint,
  getInscripcionesReglamentoEstudiantilEndpoint,
  type InteresProductoPayload,
  postInscripcionesConfirmarPreInscripcionEndpoint,
  postInscripcionesEncuestaInicialEndpoint,
  postInscripcionesInteresProductoEndpoint,
} from 'src/app/shared/api/generated/endpoints/inscripciones.endpoints';
import {
  getPersonaDocumentoEndpoint,
  getPersonaFotoEndpoint,
} from 'src/app/shared/api/generated/endpoints/persona.endpoints';
import type { AceptacionReglamentoEstudiantilResponse } from 'src/app/shared/api/generated/models/aceptacionReglamentoEstudiantilResponse';
import type { ConfirmarPreInscripcionRequest } from 'src/app/shared/api/generated/models/confirmarPreInscripcionRequest';
import type { ConfirmarPreInscripcionResponse } from 'src/app/shared/api/generated/models/confirmarPreInscripcionResponse';
import type { DocumentoPersonaResponse } from 'src/app/shared/api/generated/models/documentoPersonaResponse';
import type { DtoEncuestaInicialAdmisionResponse } from 'src/app/shared/api/generated/models/dtoEncuestaInicialAdmisionResponse';
import type { GuardarEncuestaInicialRequest } from 'src/app/shared/api/generated/models/guardarEncuestaInicialRequest';

@Injectable({
  providedIn: 'root',
})
export class InscripcionesEndpoint {
  private readonly api = inject(ApiHttpClient);

  public getIdentityDocument(): Observable<DocumentoPersonaResponse> {
    return this.api.request(getPersonaDocumentoEndpoint, { cache: false });
  }

  public getIdentityPhoto(): Observable<Blob> {
    return this.api.request(getPersonaFotoEndpoint, { cache: false, responseType: 'blob' });
  }

  public getInitialSurvey(): Observable<DtoEncuestaInicialAdmisionResponse> {
    return this.api.request(getInscripcionesEncuestaInicialEndpoint, { cache: false });
  }

  public saveInitialSurvey(payload: GuardarEncuestaInicialRequest): Observable<boolean> {
    return this.api
      .request(postInscripcionesEncuestaInicialEndpoint, {
        body: payload,
        showLoader: true,
      })
      .pipe(tap(() => this.api.clearCache()));
  }

  public getStudentRegulationAcceptance(): Observable<AceptacionReglamentoEstudiantilResponse> {
    return this.api.request(getInscripcionesReglamentoEstudiantilEndpoint, { cache: false });
  }

  public confirmPreEnrollment(
    payload: ConfirmarPreInscripcionRequest
  ): Observable<ConfirmarPreInscripcionResponse> {
    return this.api
      .request(postInscripcionesConfirmarPreInscripcionEndpoint, {
        body: payload,
        showLoader: true,
      })
      .pipe(tap(() => this.api.clearCache()));
  }

  public registerProductInterest(payload: InteresProductoPayload): Observable<boolean> {
    return this.api.request(postInscripcionesInteresProductoEndpoint, {
      body: payload,
      showLoader: true,
    });
  }
}

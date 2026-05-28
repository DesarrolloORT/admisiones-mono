import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiHttpClient } from 'src/app/shared/api/core/api-http-client';
import { postPersonaCambiarContrasenaEndpoint } from 'src/app/shared/api/generated/endpoints/persona.endpoints';

export interface ChangePasswordPayload {
  currentPassword: string;
  password: string;
}

@Injectable({
  providedIn: 'root',
})
export class ChangePasswordService {
  private readonly api = inject(ApiHttpClient);

  public changePassword(payload: ChangePasswordPayload): Observable<unknown> {
    return this.api.request(postPersonaCambiarContrasenaEndpoint, {
      body: {
        passwordActual: payload.currentPassword,
        passwordNueva: payload.password,
      },
      withCredentials: true,
    });
  }
}

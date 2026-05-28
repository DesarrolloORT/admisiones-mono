import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { AuthEndpoint, RecoverPasswordPayload } from '../endpoints/auth.endpoint';

@Injectable({
  providedIn: 'root',
})
export class PasswordActivationService {
  private readonly endpoint = inject(AuthEndpoint);

  public recoverPassword(payload: RecoverPasswordPayload): Observable<void> {
    return this.endpoint.recoverPassword(payload);
  }

  public activateLink(token: string): Observable<void> {
    return this.endpoint.activatePasswordLink({ token });
  }

  public completePassword(password: string): Observable<void> {
    return this.endpoint.completePassword({ passwordNueva: password });
  }
}

import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { map } from 'rxjs/operators';

import { AuthSessionService } from '../../features/auth/services/auth-session';

function checkAuthenticated() {
  const authSession = inject(AuthSessionService);
  const router = inject(Router);

  return authSession
    .ensureAuthenticatedSession()
    .pipe(
      map(isAuthenticated => (isAuthenticated ? true : router.createUrlTree(['/iniciar-sesion'])))
    );
}

export const authMatchGuard: CanMatchFn = () => checkAuthenticated();

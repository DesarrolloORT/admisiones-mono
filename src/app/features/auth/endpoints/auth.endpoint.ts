import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

import {
  AuthLoginRequest,
  AuthLoginResponse,
  AuthRegisterRequest,
  AuthRegisterResponse,
} from '../models/auth.interface';
import { AuthRequestError, AuthRequestOperation } from '../models/auth-error';

@Injectable({
  providedIn: 'root',
})
export class AuthEndpoint {
  private readonly http = inject(HttpClient);

  public readonly loginUrl = this.resolveUrl('/login');
  public readonly registerUrl = this.resolveUrl('/register');

  public login(payload: AuthLoginRequest): Observable<AuthLoginResponse> {
    return this.http
      .post<AuthLoginResponse>(this.loginUrl, payload, { withCredentials: true })
      .pipe(catchError(error => this.toRequestError('login', error)));
  }

  public register(payload: AuthRegisterRequest): Observable<AuthRegisterResponse> {
    return this.http
      .post<AuthRegisterResponse>(this.registerUrl, payload, {
        withCredentials: true,
      })
      .pipe(catchError(error => this.toRequestError('register', error)));
  }

  private toRequestError(operation: AuthRequestOperation, error: unknown): Observable<never> {
    const status = error instanceof HttpErrorResponse ? error.status : null;
    return throwError(() => new AuthRequestError(operation, status));
  }

  private resolveUrl(path: string): string {
    try {
      return new URL(path, environment.API_URL).toString();
    } catch {
      return path;
    }
  }
}

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from 'src/environments/environment';

import { AuthRequestError } from '../models/auth-error';
import { AuthEndpoint } from './auth.endpoint';

describe('AuthEndpoint', () => {
  let endpoint: AuthEndpoint;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), AuthEndpoint],
    });

    endpoint = TestBed.inject(AuthEndpoint);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
  });

  it('should expose resolved endpoint urls', () => {
    expect(endpoint.loginUrl).toBe(new URL('/login', environment.API_URL).toString());
    expect(endpoint.registerUrl).toBe(new URL('/register', environment.API_URL).toString());
  });

  it('should post login payload', () => {
    endpoint
      .login({
        documentType: 'CI',
        documentNumber: '12345678',
        password: 'secret',
      })
      .subscribe(response => {
        expect(response.token).toBe('token-123');
      });

    const request = httpController.expectOne(endpoint.loginUrl);
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toEqual({
      documentType: 'CI',
      documentNumber: '12345678',
      password: 'secret',
    });

    request.flush({ token: 'token-123', expiresAt: '2026-05-11T18:00:00Z' });
  });

  it('should post register payload', () => {
    const payload = {
      identity: {
        documentType: 'CI',
        documentNumber: '12345678',
      },
      personal: {
        firstName: 'Ana',
        secondName: 'Maria',
        firstLastName: 'Silva',
        secondLastName: 'Pereira',
        birthDate: '2000-01-01',
        sex: 'F',
        country: 'Uruguay',
        address: 'Mercedes 1234',
        phone: '099123456',
        email: 'ana@example.com',
        confirmEmail: 'ana@example.com',
      },
    };

    endpoint.register(payload).subscribe(response => {
      expect(response.success).toBe(true);
    });

    const request = httpController.expectOne(endpoint.registerUrl);
    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toEqual(payload);

    request.flush({ success: true });
  });

  it('should map http login errors to domain errors', () => {
    endpoint
      .login({ documentType: 'CI', documentNumber: '12345678', password: 'secret' })
      .subscribe({
        error: error => {
          expect(error).toBeInstanceOf(AuthRequestError);
          expect(error.operation).toBe('login');
          expect(error.status).toBe(401);
        },
      });

    const request = httpController.expectOne(endpoint.loginUrl);
    request.flush({}, { status: 401, statusText: 'Unauthorized' });
  });
});

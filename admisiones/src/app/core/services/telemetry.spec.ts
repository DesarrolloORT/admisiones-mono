import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse, HttpRequest, HttpResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Event as RouterEvent, NavigationEnd, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { environment } from 'src/environments/environment';

import { TelemetryService } from './telemetry';

describe('TelemetryService', () => {
  let routerEvents: Subject<RouterEvent>;
  let service: TelemetryService;

  beforeEach(() => {
    routerEvents = new Subject<RouterEvent>();
    window.sessionStorage.clear();
    window.__ADMISIONES_TELEMETRY__ = [];
    delete window.__TEST_RUN_ID__;
    window.history.replaceState(null, '', '/iniciar-sesion');

    TestBed.configureTestingModule({
      providers: [
        {
          provide: Router,
          useValue: {
            events: routerEvents.asObservable(),
          },
        },
        { provide: DOCUMENT, useValue: document },
      ],
    });

    service = TestBed.inject(TelemetryService);
  });

  afterEach(() => {
    window.sessionStorage.clear();
    window.__ADMISIONES_TELEMETRY__ = [];
    delete window.__TEST_RUN_ID__;
  });

  it('adds request correlation headers for API calls', () => {
    window.__TEST_RUN_ID__ = 'playwright-20260528-001';
    service.initialize();

    const request = new HttpRequest(
      'GET',
      new URL('/catalogs/degree-programs', environment.API_URL).toString()
    );
    const intercepted = service.addHttpHeaders(request);

    expect(intercepted.headers.get('traceparent')).toMatch(/^00-[a-f0-9]{32}-[a-f0-9]{16}-01$/);
    expect(intercepted.headers.get('baggage')).toContain('client_route=%2Finiciar-sesion');
    expect(intercepted.headers.get('x-correlation-id')).toBeTruthy();
    expect(intercepted.headers.get('x-client-session-id')).toBeTruthy();
    expect(intercepted.headers.get('x-client-route')).toBe('/iniciar-sesion');
    expect(intercepted.headers.get('x-client-route-history')).toBeNull();
    expect(intercepted.headers.get('x-client-service')).toBeNull();
    expect(intercepted.headers.get('x-client-page-age-bucket')).toMatch(
      /^(<1m|1m-5m|5m-30m|30m-2h|2h\+)$/
    );
    expect(intercepted.headers.get('x-client-device')).toContain('desktop');
    expect(intercepted.headers.get('x-test-run-id')).toBe('playwright-20260528-001');
  });

  it('does not send the route trail in normal API headers', () => {
    service.initialize();

    routerEvents.next(
      new NavigationEnd(1, '/registro?utm=campana#inicio', '/registro?utm=campana#inicio')
    );
    routerEvents.next(new NavigationEnd(2, '/inicio', '/inicio'));
    routerEvents.next(
      new NavigationEnd(
        3,
        '/inicio/datos-personales?token=secret',
        '/inicio/datos-personales?token=secret'
      )
    );
    window.history.replaceState(null, '', '/inicio/datos-personales?token=secret');

    const request = new HttpRequest(
      'POST',
      new URL('/person/details', environment.API_URL).toString(),
      null
    );
    const intercepted = service.addHttpHeaders(request);

    expect(intercepted.headers.get('x-client-route-history')).toBeNull();
    expect(intercepted.headers.get('x-client-route')).toBe('/inicio/datos-personales');
  });

  it('buffers http response and error telemetry', () => {
    service.initialize();
    const request = service.addHttpHeaders(
      new HttpRequest(
        'GET',
        new URL('/person/details?token=secret-token', environment.API_URL).toString()
      )
    );
    const startedAt = service.startHttpRequest(request);

    service.trackHttpResponse(request, new HttpResponse({ status: 200 }), startedAt);
    service.trackHttpError(
      request,
      new HttpErrorResponse({ status: 500, statusText: 'Server error' }),
      startedAt
    );

    expect(window.__ADMISIONES_TELEMETRY__).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ name: 'admisiones.http.request' }),
        expect.objectContaining({ name: 'admisiones.http.response' }),
        expect.objectContaining({ name: 'admisiones.http.error' }),
      ])
    );
    expect(JSON.stringify(window.__ADMISIONES_TELEMETRY__)).not.toContain('secret-token');
  });
});

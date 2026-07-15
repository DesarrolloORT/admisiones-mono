import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse, HttpRequest, HttpResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Event as RouterEvent, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

type TelemetryAttribute = string | number | boolean | null | undefined;
type TelemetryAttributes = Record<string, TelemetryAttribute>;
type TelemetryEnvironment = Partial<{
  production: boolean;
  API_URL: string;
  APP_VERSION: string;
  ENVIRONMENT_NAME: string;
  TELEMETRY_SERVICE_NAME: string;
}>;

export type TelemetryEvent = {
  name: string;
  timestamp: string;
  attributes: Record<string, string | number | boolean | null>;
};

declare global {
  interface Window {
    __TEST_RUN_ID__?: string;
    __ADMISIONES_TELEMETRY__?: TelemetryEvent[];
  }
}

const TELEMETRY_ENVIRONMENT = environment as TelemetryEnvironment;
const SESSION_ID_KEY = 'admisiones.telemetry.session-id';
const ROUTE_TRAIL_KEY = 'admisiones.telemetry.route-trail';
const MAX_ROUTE_TRAIL = 6;
const MAX_BUFFERED_EVENTS = 100;
const HEADER_VALUE_LIMIT = 512;

@Injectable({ providedIn: 'root' })
export class TelemetryService {
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);
  private readonly sessionId = this.resolveSessionId();
  private readonly pageStartedAt = this.now();
  private initialized = false;
  private routeTrail: string[] = [];

  public initialize(): void {
    if (this.initialized) {
      return;
    }

    this.initialized = true;
    this.routeTrail = this.restoreRouteTrail();
    this.recordRoute(this.currentRoute());

    this.router.events
      .pipe(filter((event: RouterEvent): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(event => this.recordRoute(event.urlAfterRedirects || event.url));

    this.registerErrorHandlers();
    this.captureInitialPerformance();
    this.trackEvent('admisiones.frontend.initialized');
  }

  public addHttpHeaders(request: HttpRequest<unknown>): HttpRequest<unknown> {
    if (!this.isApiRequest(request.url)) {
      return request;
    }

    return request.clone({ setHeaders: this.buildHttpHeaders() });
  }

  public startHttpRequest(request: HttpRequest<unknown>): number {
    const startedAt = this.now();

    this.trackEvent('admisiones.http.request', {
      http_method: request.method,
      http_path: this.pathOnly(request.urlWithParams),
      trace_id: this.traceIdFromRequest(request),
    });

    return startedAt;
  }

  public trackHttpResponse(
    request: HttpRequest<unknown>,
    response: HttpResponse<unknown>,
    startedAt: number
  ): void {
    this.trackEvent('admisiones.http.response', {
      http_method: request.method,
      http_path: this.pathOnly(request.urlWithParams),
      http_status_code: response.status,
      duration_ms: this.elapsedMs(startedAt),
      trace_id: this.traceIdFromRequest(request),
    });
  }

  public trackHttpError(request: HttpRequest<unknown>, error: unknown, startedAt: number): void {
    const httpError = error instanceof HttpErrorResponse ? error : null;

    this.trackEvent('admisiones.http.error', {
      http_method: request.method,
      http_path: this.pathOnly(request.urlWithParams),
      http_status_code: httpError?.status ?? null,
      duration_ms: this.elapsedMs(startedAt),
      error_name: httpError?.name ?? this.errorName(error),
      trace_id: this.traceIdFromRequest(request),
    });
  }

  public trackCacheHit(request: HttpRequest<unknown>): void {
    this.trackEvent('admisiones.http.cache_hit', {
      http_method: request.method,
      http_path: this.pathOnly(request.urlWithParams),
    });
  }

  public trackEvent(name: string, attributes: TelemetryAttributes = {}): void {
    const event: TelemetryEvent = {
      name,
      timestamp: new Date().toISOString(),
      attributes: this.cleanAttributes({
        service_name: TELEMETRY_ENVIRONMENT.TELEMETRY_SERVICE_NAME ?? 'admisiones-frontend',
        service_version: TELEMETRY_ENVIRONMENT.APP_VERSION ?? '0.0.0',
        environment: TELEMETRY_ENVIRONMENT.ENVIRONMENT_NAME ?? this.environmentName(),
        session_id: this.sessionId,
        current_route: this.currentRoute(),
        route_history: this.routeTrail.join(' > '),
        device: this.deviceSummary(),
        test_run_id: this.testRunId(),
        ...attributes,
      }),
    };

    this.bufferEvent(event);
  }

  private buildHttpHeaders(): Record<string, string> {
    const traceparent = this.createTraceparent();
    const currentRoute = this.currentRoute();
    const headers: Record<string, string> = {
      traceparent,
      baggage: this.baggageHeader(currentRoute),
      'x-correlation-id': this.sessionId,
      'x-client-session-id': this.sessionId,
      'x-client-route': currentRoute,
      'x-client-device': this.deviceSummary(),
      'x-client-environment': TELEMETRY_ENVIRONMENT.ENVIRONMENT_NAME ?? this.environmentName(),
      'x-client-page-age-bucket': this.pageAgeBucket(),
      'x-client-version': TELEMETRY_ENVIRONMENT.APP_VERSION ?? '0.0.0',
    };
    const testRunId = this.testRunId();

    if (testRunId) {
      headers['x-test-run-id'] = testRunId;
    }

    return Object.fromEntries(
      Object.entries(headers).map(([key, value]) => [key, this.headerValue(value)])
    );
  }

  private baggageHeader(currentRoute: string): string {
    const values = [
      ['correlation_id', this.sessionId],
      ['client_session_id', this.sessionId],
      ['client_route', currentRoute],
      ['client_device', this.deviceSummary()],
      ['test_run_id', this.testRunId()],
    ];

    return values
      .filter(([, value]) => !!value)
      .map(([key, value]) => `${key}=${encodeURIComponent(value ?? '')}`)
      .join(',');
  }

  private registerErrorHandlers(): void {
    const win = this.window();

    if (!win) {
      return;
    }

    win.addEventListener('error', event => {
      this.trackEvent('admisiones.javascript.error', {
        error_name: event.error instanceof Error ? event.error.name : null,
      });
    });

    win.addEventListener('unhandledrejection', event => {
      this.trackEvent('admisiones.javascript.unhandled_rejection', {
        error_name: this.errorName(event.reason),
      });
    });
  }

  private captureInitialPerformance(): void {
    const win = this.window();
    const performance = win?.performance;

    if (!win || !performance) {
      return;
    }

    const capture = (): void => {
      const navigation = performance.getEntriesByType('navigation')[0] as
        PerformanceNavigationTiming | undefined;

      if (!navigation) {
        return;
      }

      this.trackEvent('admisiones.performance.navigation', {
        duration_ms: Math.round(navigation.duration),
        dom_content_loaded_ms: Math.round(navigation.domContentLoadedEventEnd),
        load_event_ms: Math.round(navigation.loadEventEnd),
        transfer_size: navigation.transferSize || null,
      });
    };

    if (this.document.readyState === 'complete') {
      capture();
      return;
    }

    win.addEventListener('load', capture, { once: true });
  }

  private recordRoute(route: string): void {
    const cleanRoute = this.headerValue(this.routePath(route));

    if (this.routeTrail.at(-1) === cleanRoute) {
      return;
    }

    this.routeTrail = [...this.routeTrail, cleanRoute].slice(-MAX_ROUTE_TRAIL);
    this.setSessionItem(ROUTE_TRAIL_KEY, JSON.stringify(this.routeTrail));

    this.trackEvent('admisiones.navigation', {
      route: cleanRoute,
    });
  }

  private restoreRouteTrail(): string[] {
    const rawTrail = this.getSessionItem(ROUTE_TRAIL_KEY);

    if (!rawTrail) {
      return [];
    }

    try {
      const parsed = JSON.parse(rawTrail);

      return Array.isArray(parsed) ? parsed.filter(item => typeof item === 'string') : [];
    } catch {
      return [];
    }
  }

  private currentRoute(): string {
    const location = this.document.location;

    return this.routePath(`${location.pathname}${location.search}${location.hash}`);
  }

  private isApiRequest(url: string): boolean {
    const apiUrl = TELEMETRY_ENVIRONMENT.API_URL?.trim();

    if (!apiUrl) {
      return true;
    }

    try {
      const requestUrl = new URL(url, this.origin());
      const apiBaseUrl = new URL(apiUrl, this.origin());

      return requestUrl.origin === apiBaseUrl.origin;
    } catch {
      return true;
    }
  }

  private pathOnly(url: string): string {
    try {
      return new URL(url, this.origin()).pathname;
    } catch {
      return url.split('?')[0]?.split('#')[0] ?? url;
    }
  }

  private routePath(route: string): string {
    const path = this.pathOnly(route || '/');

    return path || '/';
  }

  private traceIdFromRequest(request: HttpRequest<unknown>): string | null {
    const traceparent = request.headers.get('traceparent');

    return traceparent?.split('-')[1] ?? null;
  }

  private createTraceparent(): string {
    return `00-${this.randomHex(16)}-${this.randomHex(8)}-01`;
  }

  private resolveSessionId(): string {
    const stored = this.getSessionItem(SESSION_ID_KEY);

    if (stored) {
      return stored;
    }

    const sessionId = this.createId();
    this.setSessionItem(SESSION_ID_KEY, sessionId);

    return sessionId;
  }

  private createId(): string {
    const crypto = this.window()?.crypto;

    if (crypto?.randomUUID) {
      return crypto.randomUUID();
    }

    return `session-${this.randomHex(16)}`;
  }

  private randomHex(byteCount: number): string {
    const values = new Uint8Array(byteCount);
    const crypto = this.window()?.crypto;

    if (crypto?.getRandomValues) {
      crypto.getRandomValues(values);
    } else {
      for (let index = 0; index < byteCount; index += 1) {
        values[index] = Math.floor(Math.random() * 256);
      }
    }

    const hex = Array.from(values, value => value.toString(16).padStart(2, '0')).join('');

    return /^0+$/.test(hex) ? '1'.padStart(byteCount * 2, '0') : hex;
  }

  private deviceSummary(): string {
    const navigator = this.window()?.navigator;

    if (!navigator) {
      return 'unknown';
    }

    return [this.deviceType(navigator), this.platform(navigator), this.browser(navigator)]
      .filter(value => value && value !== 'unknown')
      .join('; ');
  }

  private deviceType(navigator: Navigator): string {
    const userAgent = navigator.userAgent.toLowerCase();

    if (/ipad|tablet/.test(userAgent)) {
      return 'tablet';
    }

    if (/mobi|android|iphone|ipod/.test(userAgent)) {
      return 'mobile';
    }

    return 'desktop';
  }

  private platform(navigator: Navigator): string {
    const navigatorWithUserAgentData = navigator as Navigator & {
      userAgentData?: { platform?: string };
    };

    return navigatorWithUserAgentData.userAgentData?.platform ?? navigator.platform ?? 'unknown';
  }

  private browser(navigator: Navigator): string {
    const userAgent = navigator.userAgent;

    if (/Edg\//.test(userAgent)) {
      return 'Edge';
    }

    if (/OPR\//.test(userAgent)) {
      return 'Opera';
    }

    if (/Firefox\//.test(userAgent)) {
      return 'Firefox';
    }

    if (/Chrome\//.test(userAgent)) {
      return 'Chrome';
    }

    if (/Safari\//.test(userAgent)) {
      return 'Safari';
    }

    return 'unknown';
  }

  private testRunId(): string | null {
    return this.window()?.__TEST_RUN_ID__ ?? null;
  }

  private cleanAttributes(
    attributes: TelemetryAttributes
  ): Record<string, string | number | boolean | null> {
    return Object.fromEntries(
      Object.entries(attributes)
        .filter(([, value]) => value !== undefined)
        .map(([key, value]) => [
          key,
          typeof value === 'string' ? this.headerValue(value) : (value ?? null),
        ])
    );
  }

  private headerValue(value: string): string {
    return value.replace(/[\r\n]/g, ' ').slice(0, HEADER_VALUE_LIMIT);
  }

  private errorName(error: unknown): string {
    if (error instanceof Error) {
      return error.name;
    }

    return 'UnknownError';
  }

  private elapsedMs(startedAt: number): number {
    return Math.round(this.now() - startedAt);
  }

  private pageAgeBucket(): string {
    const ageMs = this.elapsedMs(this.pageStartedAt);
    const minute = 60_000;
    const hour = 60 * minute;

    if (ageMs < minute) {
      return '<1m';
    }

    if (ageMs < 5 * minute) {
      return '1m-5m';
    }

    if (ageMs < 30 * minute) {
      return '5m-30m';
    }

    if (ageMs < 2 * hour) {
      return '30m-2h';
    }

    return '2h+';
  }

  private now(): number {
    return this.window()?.performance?.now() ?? Date.now();
  }

  private environmentName(): string {
    return TELEMETRY_ENVIRONMENT.production ? 'production' : 'development';
  }

  private bufferEvent(event: TelemetryEvent): void {
    const win = this.window();

    if (!win) {
      return;
    }

    win.__ADMISIONES_TELEMETRY__ = [...(win.__ADMISIONES_TELEMETRY__ ?? []), event].slice(
      -MAX_BUFFERED_EVENTS
    );
  }

  private getSessionItem(key: string): string | null {
    try {
      return this.window()?.sessionStorage.getItem(key) ?? null;
    } catch {
      return null;
    }
  }

  private setSessionItem(key: string, value: string): void {
    try {
      this.window()?.sessionStorage.setItem(key, value);
    } catch {
      // Session storage is best-effort telemetry context.
    }
  }

  private origin(): string {
    return this.window()?.location.origin ?? 'http://localhost';
  }

  private window(): Window | null {
    return this.document.defaultView;
  }
}

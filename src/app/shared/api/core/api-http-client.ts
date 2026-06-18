import {
  HttpClient,
  HttpContext,
  HttpContextToken,
  HttpHeaders,
  HttpParams,
} from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { isOperationResult, unwrapOperationResultContext } from '@desarrolloort/ngx-utils';
import { Observable } from 'rxjs';
import { map, shareReplay } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

import { CAPTCHA_ACTION } from '../../../core/services/captcha-token';
import {
  ApiEndpoint,
  ApiResponseData,
  EndpointData,
  EndpointDefinition,
  EndpointListItem,
  EndpointPathParams,
  EndpointQueryParams,
  EndpointRequest,
  EndpointResponse,
} from './api-endpoint';
import { buildApiPath } from './api-path-builder';

export const SHOW_GLOBAL_LOADER = new HttpContextToken<boolean>(() => false);

export type ApiRequestOptions<TEndpoint extends ApiEndpoint<EndpointDefinition>> = {
  pathParams?: EndpointPathParams<TEndpoint>;
  queryParams?: EndpointQueryParams<TEndpoint>;
  body?: EndpointRequest<TEndpoint>;
  headers?: ApiRequestHeaders;
  withCredentials?: boolean;
  cache?: boolean;
  captchaAction?: string;
  showLoader?: boolean;
  context?: HttpContext;
  unwrapOperationResult?: boolean;
};

export type ApiRequestHeaders =
  | HttpHeaders
  | Record<string, string | number | boolean | null | undefined>;

@Injectable({ providedIn: 'root' })
export class ApiHttpClient {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.API_URL;
  private readonly getCache = new Map<string, Observable<unknown>>();

  public request<TEndpoint extends ApiEndpoint<EndpointDefinition>>(
    endpoint: TEndpoint,
    options: ApiRequestOptions<TEndpoint> = {}
  ): Observable<EndpointData<TEndpoint>> {
    const url = this.resolveUrl(buildApiPath(endpoint.path, options.pathParams));
    const params = this.buildHttpParams(options.queryParams);
    const withCredentials = this.resolveWithCredentials(endpoint, options);
    const requestOptions = {
      context: this.resolveContext(options),
      headers: this.buildHttpHeaders(options.headers),
      params,
      withCredentials,
    };

    if (this.shouldCache(endpoint, options, params)) {
      const cacheKey = this.getCacheKey(url, withCredentials);
      const cached = this.getCache.get(cacheKey) as Observable<EndpointData<TEndpoint>> | undefined;

      if (cached) {
        return cached;
      }

      const fresh = this.execute(endpoint, url, requestOptions, options.body).pipe(
        shareReplay({ bufferSize: 1, refCount: false })
      );

      this.getCache.set(cacheKey, fresh);

      return fresh;
    }

    return this.execute(endpoint, url, requestOptions, options.body);
  }

  public requestWithMessage<TEndpoint extends ApiEndpoint<EndpointDefinition>>(
    endpoint: TEndpoint,
    options: ApiRequestOptions<TEndpoint> = {}
  ): Observable<{ data: EndpointData<TEndpoint>; message: string | null }> {
    const url = this.resolveUrl(buildApiPath(endpoint.path, options.pathParams));
    const requestOptions = {
      context: this.resolveContext({ ...options, unwrapOperationResult: false }),
      headers: this.buildHttpHeaders(options.headers),
      params: this.buildHttpParams(options.queryParams),
      withCredentials: this.resolveWithCredentials(endpoint, options),
    };

    return this.executeRaw(endpoint, url, requestOptions, options.body).pipe(
      map(response => ({
        data: this.unwrapOperationResult(response),
        message: isOperationResult(response) ? (response.message ?? null) : null,
      }))
    );
  }

  public clearCache(): void {
    this.getCache.clear();
  }

  public data<TEndpoint extends ApiEndpoint<EndpointDefinition>>(
    endpoint: TEndpoint,
    options: ApiRequestOptions<TEndpoint> = {}
  ): Observable<EndpointData<TEndpoint>> {
    return this.request(endpoint, options);
  }

  public list<TEndpoint extends ApiEndpoint<EndpointDefinition>>(
    endpoint: TEndpoint,
    options?: ApiRequestOptions<TEndpoint>
  ): Observable<Array<EndpointListItem<TEndpoint>>>;
  public list<TEndpoint extends ApiEndpoint<EndpointDefinition>, TResult>(
    endpoint: TEndpoint,
    mapper: (item: EndpointListItem<TEndpoint>) => TResult,
    options?: ApiRequestOptions<TEndpoint>
  ): Observable<TResult[]>;
  public list<TEndpoint extends ApiEndpoint<EndpointDefinition>, TResult>(
    endpoint: TEndpoint,
    mapperOrOptions?:
      | ((item: EndpointListItem<TEndpoint>) => TResult)
      | ApiRequestOptions<TEndpoint>,
    options?: ApiRequestOptions<TEndpoint>
  ): Observable<Array<EndpointListItem<TEndpoint>> | TResult[]> {
    const mapper = typeof mapperOrOptions === 'function' ? mapperOrOptions : null;
    const requestOptions = typeof mapperOrOptions === 'function' ? options : mapperOrOptions;

    return this.data(endpoint, requestOptions).pipe(
      map(data => {
        const items = this.toList(data) as Array<EndpointListItem<TEndpoint>>;
        return mapper ? items.map(mapper) : items;
      })
    );
  }

  private execute<TEndpoint extends ApiEndpoint<EndpointDefinition>>(
    endpoint: TEndpoint,
    url: string,
    requestOptions: {
      context: HttpContext;
      headers?: HttpHeaders;
      params?: HttpParams;
      withCredentials?: boolean;
    },
    body?: EndpointRequest<TEndpoint>
  ): Observable<EndpointData<TEndpoint>> {
    const response$ = this.executeRaw(endpoint, url, requestOptions, body);

    return response$.pipe(map(response => this.unwrapOperationResult(response)));
  }

  private executeRaw<TEndpoint extends ApiEndpoint<EndpointDefinition>>(
    endpoint: TEndpoint,
    url: string,
    requestOptions: {
      context: HttpContext;
      headers?: HttpHeaders;
      params?: HttpParams;
      withCredentials?: boolean;
    },
    body?: EndpointRequest<TEndpoint>
  ): Observable<EndpointResponse<TEndpoint>> {
    switch (endpoint.method) {
      case 'GET':
        return this.http.get<EndpointResponse<TEndpoint>>(url, requestOptions);
      case 'POST':
        return this.http.post<EndpointResponse<TEndpoint>>(url, body, requestOptions);
      case 'PUT':
        return this.http.put<EndpointResponse<TEndpoint>>(url, body, requestOptions);
      case 'PATCH':
        return this.http.patch<EndpointResponse<TEndpoint>>(url, body, requestOptions);
      case 'DELETE':
        return this.http.delete<EndpointResponse<TEndpoint>>(url, requestOptions);
    }
  }

  private resolveContext<TEndpoint extends ApiEndpoint<EndpointDefinition>>(
    options: ApiRequestOptions<TEndpoint>
  ): HttpContext {
    let context = options.context ?? new HttpContext();
    const captchaAction = options.captchaAction?.trim();

    if (captchaAction) {
      context = context.set(CAPTCHA_ACTION, captchaAction);
    }

    if (options.showLoader) {
      context = context.set(SHOW_GLOBAL_LOADER, true);
    }

    if (options.unwrapOperationResult === false) {
      return context;
    }

    return unwrapOperationResultContext(context);
  }

  private resolveUrl(path: string): string {
    try {
      return new URL(path, this.apiBaseUrl).toString();
    } catch {
      return `${this.apiBaseUrl}${path}`;
    }
  }

  private buildHttpParams(queryParams: unknown): HttpParams | undefined {
    if (!queryParams || typeof queryParams !== 'object') {
      return undefined;
    }

    let params = new HttpParams();

    for (const [key, value] of Object.entries(queryParams)) {
      params = this.appendHttpParam(params, key, value);
    }

    return params;
  }

  private buildHttpHeaders(headers: ApiRequestHeaders | undefined): HttpHeaders | undefined {
    if (!headers) {
      return undefined;
    }

    if (headers instanceof HttpHeaders) {
      return headers;
    }

    let httpHeaders = new HttpHeaders();

    for (const [key, value] of Object.entries(headers)) {
      if (value === undefined || value === null) {
        continue;
      }

      httpHeaders = httpHeaders.set(key, String(value));
    }

    return httpHeaders;
  }

  private appendHttpParam(params: HttpParams, key: string, value: unknown): HttpParams {
    if (value === undefined || value === null) {
      return params;
    }

    if (Array.isArray(value)) {
      return value.reduce(
        (currentParams, item) => this.appendHttpParam(currentParams, key, item),
        params
      );
    }

    return params.append(key, String(value));
  }

  private shouldCache<TEndpoint extends ApiEndpoint<EndpointDefinition>>(
    endpoint: TEndpoint,
    options: ApiRequestOptions<TEndpoint>,
    params: HttpParams | undefined
  ): boolean {
    return (
      options.cache !== false &&
      endpoint.method === 'GET' &&
      !this.hasObjectValues(options.pathParams) &&
      !params?.keys().length
    );
  }

  private hasObjectValues(value: unknown): boolean {
    return !!value && typeof value === 'object' && Object.keys(value).length > 0;
  }

  private getCacheKey(url: string, withCredentials: boolean | undefined): string {
    return `${withCredentials ? 'credentials' : 'default'} ${url}`;
  }

  private resolveWithCredentials<TEndpoint extends ApiEndpoint<EndpointDefinition>>(
    endpoint: TEndpoint,
    options: ApiRequestOptions<TEndpoint>
  ): boolean | undefined {
    if (options.withCredentials !== undefined) {
      return options.withCredentials;
    }
    return endpoint.requiresAuth ? true : undefined;
  }

  private unwrapOperationResult<TResponse>(response: TResponse): ApiResponseData<TResponse> {
    if (isOperationResult(response)) {
      return response.data as ApiResponseData<TResponse>;
    }

    return response as ApiResponseData<TResponse>;
  }

  private toList<TItem>(data: TItem | TItem[] | null | undefined): TItem[] {
    if (data === undefined || data === null) {
      return [];
    }

    return Array.isArray(data) ? data : [data];
  }
}

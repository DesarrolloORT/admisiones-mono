export type ApiHttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

export type EndpointDefinition = {
  pathParams: unknown;
  queryParams: unknown;
  request: unknown;
  response: unknown;
};

export type ApiEndpoint<TDefinition extends EndpointDefinition = EndpointDefinition> = {
  readonly operationId: string;
  readonly method: ApiHttpMethod;
  readonly path: string;
  readonly requiresAuth?: boolean;
  readonly __types?: TDefinition;
};

export function defineEndpoint<TDefinition extends EndpointDefinition>(
  endpoint: Omit<ApiEndpoint<TDefinition>, '__types'>
): ApiEndpoint<TDefinition> {
  return endpoint;
}

export type EndpointPathParams<TEndpoint> =
  TEndpoint extends ApiEndpoint<infer TDefinition> ? TDefinition['pathParams'] : never;

export type EndpointQueryParams<TEndpoint> =
  TEndpoint extends ApiEndpoint<infer TDefinition> ? TDefinition['queryParams'] : never;

export type EndpointRequest<TEndpoint> =
  TEndpoint extends ApiEndpoint<infer TDefinition> ? TDefinition['request'] : never;

export type EndpointResponse<TEndpoint> =
  TEndpoint extends ApiEndpoint<infer TDefinition> ? TDefinition['response'] : never;

export type ApiResponseData<TResponse> = TResponse extends { data?: infer TData }
  ? NonNullable<TData>
  : TResponse;

export type EndpointData<TEndpoint> = ApiResponseData<EndpointResponse<TEndpoint>>;

export type EndpointListItem<TEndpoint> =
  EndpointData<TEndpoint> extends ReadonlyArray<infer TItem> ? TItem : EndpointData<TEndpoint>;


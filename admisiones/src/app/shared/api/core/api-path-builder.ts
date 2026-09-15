const PATH_PARAM_PATTERN = /\{([^}]+)\}/g;

export function buildApiPath(path: string, pathParams: unknown): string {
  PATH_PARAM_PATTERN.lastIndex = 0;

  if (!PATH_PARAM_PATTERN.test(path)) {
    return path;
  }

  if (!pathParams || typeof pathParams !== 'object') {
    throw new Error(`Missing path params for API path: ${path}`);
  }

  PATH_PARAM_PATTERN.lastIndex = 0;

  return path.replace(PATH_PARAM_PATTERN, (_placeholder, key: string) => {
    if (!Object.hasOwn(pathParams, key)) {
      throw new Error(`Missing path param "${key}" for API path: ${path}`);
    }

    const value = (pathParams as Record<string, unknown>)[key];
    if (value === undefined || value === null) {
      throw new Error(`Missing path param "${key}" for API path: ${path}`);
    }

    return encodeURIComponent(String(value));
  });
}

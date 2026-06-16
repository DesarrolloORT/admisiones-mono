import { generatedEnvironment } from './environment.generated';

export const environment = {
  production: generatedEnvironment.environment === 'production',
  // API_URL: 'https://localhost:7150',
  API_URL: 'https://apiadmisionesdesa.ort.edu.uy',
  RECAPTCHA_KEY: '6Lce7l8pAAAAAI3De9pYcKJSfru4J7PDPVePZLe3',
  CSP_POLICY: generatedEnvironment.cspPolicy,
  ENVIRONMENT_NAME: generatedEnvironment.environment,
  CACHING_ENABLED: generatedEnvironment.cachingEnabled === 'true',
  APP_VERSION: generatedEnvironment.appVersion ?? '0.0.0',
  TELEMETRY_SERVICE_NAME: generatedEnvironment.telemetryServiceName ?? 'admisiones-frontend',
};

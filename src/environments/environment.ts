import { generatedEnvironment } from './environment.generated';

export const environment = {
  production: generatedEnvironment.environment === 'production',
  API_URL: generatedEnvironment.apiUrl,
  RECAPTCHA_KEY: generatedEnvironment.recaptchaSiteKey,
  CSP_POLICY: generatedEnvironment.cspPolicy,
  ENVIRONMENT_NAME: generatedEnvironment.environment,
  CACHING_ENABLED: generatedEnvironment.cachingEnabled === 'true',
  APP_VERSION: generatedEnvironment.appVersion ?? '0.0.0',
  TELEMETRY_SERVICE_NAME: generatedEnvironment.telemetryServiceName ?? 'admisiones-frontend',
};


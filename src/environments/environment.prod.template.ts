const API_BASE = '';

export const environment = {
  production: true,
  RECAPTCHA_ENTERPRISE_KEY: '',
  CACHING_ENABLED: true,
  CSP_POLICY:
    "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; img-src 'self' data: https://i.postimg.cc; font-src 'self' https://fonts.gstatic.com; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'self';",
  API_ORT: API_BASE,
  API_ORT_URL: `${API_BASE}/ORT/`,
  API_ORT_SECURE_URL: `${API_BASE}/ORTSecure/`,
};


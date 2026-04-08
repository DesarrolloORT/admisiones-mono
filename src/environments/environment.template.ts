export const environment = {
  production: false,
  CACHING_ENABLED: true,
  //Add specific CSP for production, if needed.
  CSP_POLICY:
    "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; img-src 'self' data: https://i.postimg.cc; font-src 'self' https://fonts.gstatic.com; connect-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'self';",
  RECAPTCHA_KEY: '',
  API_URL: '',
  // OTRA_KEY: '',
};


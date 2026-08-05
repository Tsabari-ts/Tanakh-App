export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001',
  enableServiceWorker: false,
  logLevel: 'debug' as const,
  // Hidden admin panel path - never linked from the app UI. Dev-only
  // literal; production value is a build-time secret, see
  // environment.production.ts.
  adminRoutePath: 'admin-x9k2',
};

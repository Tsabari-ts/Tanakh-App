// TODO(LAUNCH): apiUrl is a placeholder — no production domain exists yet.
// Until a real API host is chosen, the production build points at the local API
// so that `ng build --configuration production` remains fully testable on localhost.
// See docs/LAUNCH-CHECKLIST.md, item L-01.
//
// TODO(LAUNCH): adminRoutePath is a placeholder for the same reason - it
// MUST be overwritten with the real secret path (matching the backend's
// ADMIN_ROUTE_PATH expectations, i.e. whatever value ops picks) by the
// deploy pipeline before this build is ever published, the same way apiUrl
// needs a real value substituted in. Never ship this literal to production.
export const environment = {
  production: true,
  apiUrl: 'https://localhost:5001',
  enableServiceWorker: true,
  logLevel: 'error' as const,
  adminRoutePath: 'admin-x9k2',
};

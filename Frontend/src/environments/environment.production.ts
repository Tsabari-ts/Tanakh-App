// TODO(LAUNCH): apiUrl is a placeholder — no production domain exists yet.
// Until a real API host is chosen, the production build points at the local API
// so that `ng build --configuration production` remains fully testable on localhost.
// See docs/LAUNCH-CHECKLIST.md, item L-01.
export const environment = {
  production: true,
  apiUrl: 'https://localhost:5001',
  enableServiceWorker: true,
  logLevel: 'error' as const,
};

// There is no login/session in this app - the manage token returned by
// POST /api/v1/subscriptions at signup time is the *only* thing that lets
// this browser later view/change/cancel its own SMS reminder subscription
// (GET/POST /api/v1/subscriptions/me, POST .../me/unsubscribe). Losing it
// (cleared storage, different device) means falling back to signing up
// again - there is no recovery flow, by design (see the SMS migration spec).
const MANAGE_TOKEN_KEY = 'tanach.reminderManageToken';

export function getStoredManageToken(): string | null {
  return localStorage.getItem(MANAGE_TOKEN_KEY);
}

export function setStoredManageToken(token: string): void {
  localStorage.setItem(MANAGE_TOKEN_KEY, token);
}

export function clearStoredManageToken(): void {
  localStorage.removeItem(MANAGE_TOKEN_KEY);
}

export function hasStoredManageToken(): boolean {
  return getStoredManageToken() !== null;
}

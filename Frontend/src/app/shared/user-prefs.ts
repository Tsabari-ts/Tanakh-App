// localStorage key for the one-time name entered at the entrance gate.
// Read by the home greeting and used to prefill the subscribe dialog.
export const USERNAME_KEY = 'tanach.username';

export function getStoredUsername(): string {
  return localStorage.getItem(USERNAME_KEY) || '';
}

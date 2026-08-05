// Strips undefined/null/empty-string values so HttpClient's `params` option
// doesn't send e.g. "search=" for an unset filter. Accepts any plain query
// object (page/limit numbers, optional string filters) without requiring
// callers' interfaces to declare an index signature.
export function toQueryParams<T extends object>(query: T): Record<string, string> {
  const params: Record<string, string> = {};
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== null && value !== '') {
      params[key] = String(value);
    }
  }
  return params;
}

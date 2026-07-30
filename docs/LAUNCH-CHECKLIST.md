# Launch Checklist

Items blocked on infrastructure that doesn't exist yet (production domain, hosting, third-party accounts). Every `TODO(LAUNCH)` marker in the codebase must correspond to a row here, and every row must be closed before going live.

Verify before launch: `grep -rn "TODO(LAUNCH)" Frontend/src/`

| ID | Item | Where | Added by | Status |
|---|---|---|---|---|
| L-01 | Set the real production API URL | `Frontend/src/environments/environment.production.ts` → `apiUrl` | F-06 | Open |
| L-04 | Choose and wire an error monitoring service (Sentry / App Insights / etc.), or explicitly decide against one | `Frontend/src/app/core/global-error-handler.ts` → `report()` | F-16 | Open |

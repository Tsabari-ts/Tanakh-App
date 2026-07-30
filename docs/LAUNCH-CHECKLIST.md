# Launch Checklist

Items blocked on infrastructure that doesn't exist yet (production domain, hosting, third-party accounts). Every `TODO(LAUNCH)` marker in the codebase must correspond to a row here, and every row must be closed before going live.

Verify before launch: `grep -rn "TODO(LAUNCH)" Frontend/src/`

| ID | Item | Where | Added by | Status |
|---|---|---|---|---|
| L-01 | Set the real production API URL | `Frontend/src/environments/environment.production.ts` → `apiUrl`, **and** `Frontend/ngsw-config.json` → `dataGroups[].urls` (hardcoded `https://localhost:44308`, can't reference `environment.ts` since it's a static JSON file) | F-06, extended by F-12 | Open |
| L-04 | Choose and wire an error monitoring service (Sentry / App Insights / etc.), or explicitly decide against one | `Frontend/src/app/core/global-error-handler.ts` → `report()` | F-16 | Open |
| L-05 | Set up CI running `npm run verify` (no frontend workflow exists yet — only `backend-ci.yml`/`backend-backup.yml`). Note `npm run verify` currently fails at the test step due to the pre-existing test-setup gap noted in the Baseline; that needs fixing too, or the CI gate will always be red. | repo (`.github/workflows/`), `Frontend/package.json` → `"verify"` script | F-10 | Open |

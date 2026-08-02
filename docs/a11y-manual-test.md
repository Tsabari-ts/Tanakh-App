# Accessibility manual test log

Tracks the human, screen-reader-in-hand testing (N-29) referenced by the accessibility statement's "מועד הבדיקה האחרון" field. Automated coverage (axe-core, Lighthouse, keyboard-only smoke test) lives in `Frontend/e2e/a11y.spec.ts` and `Frontend/lighthouserc.json`, and both were actually run — see status below. The screen-reader sessions in this file were **not** performed by an AI agent; a human needs to run them with a real screen reader before the statement's "last tested" date can honestly reflect that.

## Automated coverage — done 2026-08-02

| Check | Tool | Result |
|---|---|---|
| axe-core, all routes × 4 a11y modes (default / high-contrast / inverted-contrast / 150% font) | `@axe-core/playwright` | 27/27 passing |
| Accessibility score ≥ 95 on `/home`, `/settings` | Lighthouse CI | passing |
| `outline:none`, `!important` (outside sanctioned files), `px` on `font-size` | stylelint | 0 violations |
| Production build | `ng build --configuration production` | succeeds (bundle-size warning only, no error) |

Lighthouse and axe-core between them catch roughly the automatable third of WCAG failures (missing alt text, contrast, missing labels, ARIA misuse, etc.) — they cannot tell you whether the experience actually makes sense to someone using a screen reader. The scenarios below are what's still needed.

## Manual scenarios — not yet run

| Environment | Priority |
|---|---|
| NVDA + Chrome (Windows) | Required |
| VoiceOver + Safari (iOS) | Required |
| VoiceOver + Safari (macOS) | Recommended |

For each environment, with no mouse:

1. **Signup form** (`/settings` → הירשם לתזכורת יומית) — fill it out including at least one validation error, confirm the error is announced and the field is easy to find and fix.
2. **Home → book list → chapter list → chapter** navigation.
3. **Read a full chapter**, including moving to the next chapter via the floating nav.
4. **Widget**: open the floating accessibility button, cycle through the font-size and contrast radiogroups with arrow keys, toggle a few switches, confirm each change is announced, close with Escape.
5. **Accessibility statement**: open from the footer, read it, close it. Also try the `?a11y=statement` deep link directly.
6. **Welcome modal / read-permission / subscribe dialogs**: open each, Tab through it, confirm focus can't escape to the page behind it, confirm the close button is announced correctly (this was a real bug fixed during implementation — the icon used to carry the label instead of the button), confirm focus returns to whatever opened it.

## Template for recording a run

```
### <date> — <tool> + <browser>
Tester:
Scenario:
Result: pass / issues found
Notes:
```

Once a full pass across the required environments comes back clean (or with only known, tracked issues), update:
- The table above.
- `AccessibilityStatementDialogComponent`'s "מועד הבדיקה האחרון" and "הבדיקה בוצעה על ידי" fields (`Frontend/src/app/shared/a11y/accessibility-statement-dialog/accessibility-statement-dialog.component.html`).

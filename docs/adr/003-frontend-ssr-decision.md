# ADR 003: SSR/SSG for the frontend — recommend full SSG, defer implementation

## Status

Accepted (2026-07-31). Owner approved deferring implementation until a
production domain exists; SSG (not SSR) is the settled target approach
for when it's built.

## Context

The Tanach content (39 books, ~929 chapters) is entirely static and never
changes. Today the app is a pure client-side SPA: `view-source:` on any
chapter URL shows `<app-root></app-root>` and nothing else until JS runs.
That means:

- Chapter pages are effectively invisible to search engines that don't
  execute JS (and even for those that do, first-contentful-paint is slower
  than it needs to be for content this static).
- No Open Graph previews when a chapter link is shared (e.g. to WhatsApp).
- Screen readers and other assistive tech get nothing until hydration.

Angular's SSR/prerender tooling (`@angular/ssr`) can solve all of this for
content that's known at build time — which describes every chapter page in
this app.

**Crucially: there is no production domain or hosting decision yet** (see
`docs/LAUNCH-CHECKLIST.md`). Prerendering is fully buildable and testable
locally regardless — `ng build` would emit static HTML per chapter, and
`http-server dist/.../browser` can serve and inspect it — but SSR/SSG's
actual payoff (search engines actually indexing real pages, real Open Graph
previews on real shares) can't be realized until the app is public at a
real URL. Implementing this now would be real, working code with no way to
observe its actual benefit for however long it takes to get a domain.

## Decision

**When implemented, use full SSG (build-time prerendering), not dynamic
SSR.** The content is static, there are no logged-in users on reading
pages, and there is no reason to run a Node server at request time.
Prerendering delivers the same SEO/speed benefit as SSR at zero runtime
operational cost, and produces plain static HTML files — which keeps the
eventual hosting choice maximally open (any static host works; a Node
server is not required). A hybrid is the right shape: prerender the
content routes (`/books/**`), leave the dynamic-ish routes (`/settings`,
anything reminder-related if a frontend for that is ever built) as CSR.

**But: defer the implementation itself until a production domain exists,
or until it's clear one will exist soon.** Recording the decision now
rather than drifting into "we'll get to it" — the target approach (SSG,
not SSR) is settled — but not writing the ~929-page prerender-route
generator, the `afterNextRender()`/`isPlatformBrowser` sweep across every
component that touches `window`/`document`/`localStorage`, the sitemap
generator, and the per-chapter meta-tag wiring against a codebase that's
still mid-modernization (this ADR is being written partway through the
Phase 7 spec; F-14, F-03, F-04 all touched exactly the routing/state code
SSR implementation would need to interact with).

## Trade-off accepted

Chapter pages stay invisible to search engines and unshared-with-preview
until this is actually implemented — a real, ongoing cost for an app whose
whole value proposition is being found and read. Choosing to accept that
cost now rather than build SSR against a moving target, and rather than
build it with no way to verify the thing it's for (a domain existing).

## When to revisit

Implement per this decision (full SSG, hybrid with CSR for non-content
routes) as soon as either:

- A production domain/hosting choice is made (the natural trigger — SSG's
  entire payoff activates at that point), or
- Someone wants to verify the SEO/OG benefit is real before committing to
  a domain, in which case it's still fully buildable and testable locally
  first (per §0 of the spec) — `ng build`, inspect `view-source:` on the
  local static output, confirm real chapter text appears server-rendered,
  *then* make the domain decision with working proof in hand.

Absolute-URL dependencies (canonical links, `og:url`, sitemap entries) read
from a new `environment.siteUrl` field, defaulted to
`http://localhost:8080` until a real domain exists — tracked as
`docs/LAUNCH-CHECKLIST.md` item L-02 once this lands.

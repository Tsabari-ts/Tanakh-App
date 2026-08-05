# ADR 001: Reminder scheduling mechanism

## Status

Accepted. Implemented in `Backend/Tanakh.Infrastructure/Reminders/` (`ReminderPlannerService`, `ReminderDispatcherService`).

## Context

Phase 4 needs to send each subscriber a daily reminder at their chosen local
time - originally by email, later switched to SMS via SMS4FREE (see the SMS
migration spec); the scheduling mechanism itself didn't change between the
two, only what happens at the "send" step. The app's load is tiny (a niche
reading app, not a bulk mailer),
but the mechanism still needs to survive restarts, support running more than
one API instance without double-sending, and be easy to inspect when
something goes wrong ("why didn't Dana get her reminder yesterday?").

## Decision

An **outbox table + polling `BackgroundService`**, hosted inside the API
process:

- `reminder_deliveries` is the outbox: one row per subscriber per scheduled
  send, with a `status` state machine (`pending` → `sending` → `sent` /
  `failed` / `skipped`).
- `ReminderPlannerService` runs once at startup and then daily, inserting
  tomorrow's (practically: the next occurrence of each subscriber's
  preferred time) row via `INSERT ... ON CONFLICT (idempotency_key) DO
  NOTHING`.
- `ReminderDispatcherService` polls every `DispatchIntervalSeconds` (60s
  default), claims due rows with `SELECT ... FOR UPDATE SKIP LOCKED`, and
  sends them.

Every scheduled and sent message is a queryable row in Postgres - no separate
dashboard is needed to answer "what happened to this subscriber's reminder",
and state survives a process restart because it never lived only in memory.

## Rejected alternatives

**Hangfire.** Gives a built-in dashboard, retry policies, and persistent
scheduling out of the box. Rejected because it adds a runtime dependency and
its own schema/tables for a job model this app doesn't otherwise need, and
its dashboard's value is already replaced by directly querying
`reminder_deliveries` (and, longer-term, T-23's admin dashboard).

**Quartz.NET.** Only justified if complex cron expressions or job
dependency graphs are needed. This app has exactly one recurring job
(the planner, on a fixed daily schedule) and one polling loop (the
dispatcher) - Quartz's scheduling generality isn't needed and isn't
worth the added dependency and configuration surface.

## Consequences

- Multi-instance safety is mandatory, not optional, and is provided by
  `SELECT ... FOR UPDATE SKIP LOCKED` in the dispatcher's claim query
  (see ADR 002) rather than by a job-scheduling library's own locking.
- Retry/backoff, lateness handling, and the Shabbat/holiday block are all
  plain application code operating on `reminder_deliveries` rows, not
  library-provided middleware - more code, but all of it is inspectable
  and testable without a library's abstractions in the way.
- If load ever grows enough that dispatch cadence or claim contention
  becomes a real bottleneck, the mechanism can be extracted into a
  separate worker process (see ADR 002) without changing the schema.

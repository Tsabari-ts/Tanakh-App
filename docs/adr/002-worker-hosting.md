# ADR 002: Hosting the reminder scheduler inside the API process

## Status

Accepted. `ReminderPlannerService` and `ReminderDispatcherService` are
registered as `IHostedService`s in `Backend/Tanakh.Api/Program.cs`, alongside
the existing `RetentionHostedService`.

## Context

The planner and dispatcher (ADR 001) need to run somewhere. The two options
are: inside the existing API process, or as a separate worker
process/deployment.

## Decision

Host both as `BackgroundService`s inside the API process, the same way
`RetentionHostedService` already runs.

## Trade-off accepted

This is simpler to deploy - one process, one deployment pipeline, no new
infrastructure to provision or monitor - but it **requires** the distributed
claim mechanism from ADR 001 (`SELECT ... FOR UPDATE SKIP LOCKED`) the moment
more than one API instance is running. Scaling the API horizontally for
request load automatically scales the planner/dispatcher too; that's fine
by construction (both are safe under concurrent execution), but it does mean
N API instances means N dispatchers polling every 60 seconds, not one.

## When to revisit

Extract the scheduler into a separate worker process/deployment if:

- API autoscaling starts running enough instances that redundant polling
  becomes a real cost or contention concern, or
- the dispatcher's own resource usage (SMTP send latency, rate limiting)
  starts interfering with the API's request-handling latency, or
- the two need independent deploy/rollback cadences (e.g. shipping a
  planner fix without redeploying the API).

None of these apply at the current scale (a niche reading app), so the
simpler single-process deployment is the right default until one does.

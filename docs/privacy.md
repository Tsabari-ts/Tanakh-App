# Privacy & data retention

Amendment 13 to the Israeli Privacy Protection Law requires (among other
things) that consent be provable and that data not be retained longer than
necessary. This document is the single reference for how this app meets
both requirements — see the referenced tables/services for implementation.

## Consent

`consent_records` (D-09) is the append-only proof of when and how consent
was given, per `consent_type` (`marketing`/`analytics`/`functional`).
Revoking consent means inserting a **new** row with `granted = false` —
existing rows are never updated or deleted (enforced by a DB trigger, not
just app-layer convention; see the `ConsentRecords` migration). To answer
"when did subscriber X consent to what, under which policy version":

```sql
SELECT consent_type, granted, granted_at, policy_version
FROM consent_records
WHERE subscriber_id = :id
ORDER BY granted_at DESC;
```

## Suppression

`suppression_list` (D-08) is the permanent, global send-block list — hard
bounces, spam complaints, explicit removal requests. No email is ever sent
to an address on this list, enforced structurally inside `EmailSender`
(callers cannot opt out of the check). Addresses are stored as a **keyed
hash** (`IHashingService.HashEmail`, HMAC-SHA256 with a pepper from
configuration — see `Hashing:Pepper` in `Backend/README.md`), never
plaintext, so the block list itself can't leak addresses and "right to be
forgotten" can be honored without ever risking emailing the person again.

## Retention

`RetentionHostedService` (D-14, `Tanakh.Infrastructure/Retention`) runs on a
schedule (`Retention:RunInterval`, default 24h) and enforces:

| Data | Window | Action | Config key |
|---|---|---|---|
| `reminder_deliveries` | 90 days from `created_at` | Hard delete | `Retention:ReminderDeliveriesRetentionDays` |
| `email_events` | 180 days from `received_at` | Hard delete | `Retention:EmailEventsRetentionDays` |
| `subscribers` where `status = unsubscribed` | 12 months from `unsubscribed_at` | **Anonymize**, not delete (see below) | `Retention:UnsubscribedSubscriberRetentionMonths` |

All three run in batches (`Retention:BatchSize`, default 5000 rows, with a
delay between batches) to avoid long locks/table bloat, and every run logs
the table, row count, and duration.

**`suppression_list` rows are never subject to retention — they're kept
indefinitely.** This is deliberate: a bounced/complained/unsubscribed
address must never be emailed again, permanently, even after every other
trace of that subscriber is gone.

## Anonymization vs. deletion

`ISubscriberAnonymizationService` (D-15,
`Tanakh.Infrastructure/Services/SubscriberAnonymizationService.cs`)
replaces `email` with a tombstone (`deleted-{id}@anonymized.invalid`) and
nulls `display_name`, but **keeps the subscriber row** — this is why
`consent_records.subscriber_id` and `reading_progress`/
`reminder_deliveries.subscriber_id` use `ON DELETE RESTRICT` /
`ON DELETE CASCADE` rather than assuming the subscriber row disappears:
`consent_records` in particular must survive as legal proof of consent
even after the subscriber it refers to is anonymized, which a hard delete
would make impossible (a foreign key can't point at a row that no longer
exists). The row is retained for aggregate statistics; there is nothing
personally identifying left in it once anonymized.

The retention sweep skips subscribers whose email already has the
`deleted-` prefix, so an anonymized row is not reprocessed on every run.

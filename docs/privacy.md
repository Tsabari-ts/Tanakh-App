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

Reminders are sent by SMS (SMS4FREE), not email — there is no suppression
list, bounce webhook, or email-hash mechanism anymore (the old
`suppression_list`/`email_events` tables and the email-era
`ISuppressionService` were removed along with the rest of the email
subsystem). SMS4FREE returns a synchronous status code per send instead;
see `Backend/README.md` and `Sms4FreeSmsSender` for how failures are
classified and logged.

## Retention

`RetentionHostedService` (D-14, `Tanakh.Infrastructure/Retention`) runs on a
schedule (`Retention:RunInterval`, default 24h) and enforces:

| Data | Window | Action | Config key |
|---|---|---|---|
| `reminder_deliveries` | 90 days from `created_at` | Hard delete | `Retention:ReminderDeliveriesRetentionDays` |
| `subscribers` where `status = unsubscribed` | 12 months from `unsubscribed_at` | **Anonymize**, not delete (see below) | `Retention:UnsubscribedSubscriberRetentionMonths` |

Both run in batches (`Retention:BatchSize`, default 5000 rows, with a
delay between batches) to avoid long locks/table bloat, and every run logs
the table, row count, and duration.

## Anonymization vs. deletion

`ISubscriberAnonymizationService` (D-15,
`Tanakh.Infrastructure/Services/SubscriberAnonymizationService.cs`)
nulls `phone_number` and `display_name`, but **keeps the subscriber row** —
this is why `consent_records.subscriber_id` and `reading_progress`/
`reminder_deliveries.subscriber_id` use `ON DELETE RESTRICT` /
`ON DELETE CASCADE` rather than assuming the subscriber row disappears:
`consent_records` in particular must survive as legal proof of consent
even after the subscriber it refers to is anonymized, which a hard delete
would make impossible (a foreign key can't point at a row that no longer
exists). The row is retained for aggregate statistics; there is nothing
personally identifying left in it once anonymized. `phone_number`'s unique
index allows this safely — Postgres treats multiple `NULL`s as distinct, so
nulling it out on one row never collides with another.

The retention sweep skips subscribers whose `phone_number` is already
`NULL`, so an anonymized row is not reprocessed on every run.

# Message replies and visitor analytics

## Deployment

This release requires migration `20261021202150_AddMessageReplyAndVisitorStats` before the new application is published. It adds three tables, reply status columns and indexes without removing existing application data.

For a database already at `20261018041651_CustomerPrivacyAcknowledgement`, run `docs/deployment/AddMessageReplyAndVisitorStats.sql` in the hosting SQL panel. The generated script is idempotent and records its migration in `__EFMigrationsHistory`. For a database at an earlier version, apply the complete EF migration chain with `dotnet ef database update --project DataAccesLayer --startup-project WebUI` using the target environment's connection configuration. Existing deployment initialization also supports running migrations when explicitly enabled; do not enable demo data in production.

Then Pull latest and Build & publish `WebUI\WebUI.csproj`. No new SMTP credentials are needed if the existing SMTP settings work. No production migration or real visitor email was executed during implementation.

## Replies

AdminMessage is available to active System Admin and Staff accounts. An authorized MediatR command persists a reply and updates the original contact together. Recipient and author are server-derived, not accepted from form fields. A unique request ID makes network retries idempotent. Replies remain in the inbox after their status changes; inbox/history are paginated.

The reply itself is the durable mail intent. Hangfire sends it with a five-minute atomic DB lease, a 90-second operation deadline and a stable Message-ID. Only successful SMTP acceptance sets `IsMailSent`. Three automatic retries follow the initial attempt. The recovery job runs every ten minutes and re-enqueues pending intents after queue outages. Exhausted attempts remain visible in message history and Hangfire for operator review/retry. SMTP acceptance and the final DB update cannot be one atomic transaction; a process crash between them can still duplicate an email, despite the stable Message-ID.

Reply HTML is encoded plain text in the green/gold XSPA template. The AJAX form retains drafts on errors and distinguishes a saved reply from a failed history refresh. It never automatically resubmits a mutation. History periodically checks pending delivery status for up to one minute while visible. Without JavaScript, normal CSRF-protected form posting remains available.

## Measurements

Only successful HTML GETs on an explicit list of public routes are counted. Admin/member/authentication routes, API/static resources, AJAX fragments, errors and recognizable bots are excluded. This is approximate operational measurement, not fraud-proof audience identification.

No analytics cookie, raw IP, query string, referrer or complete User-Agent is stored. An HMAC keyed with `Analytics:HashKey` (or the existing validated `Jwt:SigningKey` as fallback) includes a purpose and local calendar day. Keep this key private and stable; changing it during a day changes that day's identifiers. Shared IPs can merge visitors, and address changes can split them. Only a day-specific hash, page path, mobile/desktop category and UTC timestamp are retained.

Page views are append-only durable database events, avoiding lost concurrent counter increments or losing an in-memory batch during restart. Dashboard snapshots are cached for 30 seconds; completed-day snapshots use durable summary rows. The nightly `daily-visitor-aggregation` job runs at 00:05 Türkiye time using Hangfire's five-field `Cron.Daily(0, 5)`, not a Quartz six-field expression. It also finalizes missed days, removes raw records older than 30 days only after their summaries exist, and warms today's/yesterday's caches. Because cleanup is daily, retention is approximately 30 days. Daily aggregate summaries are retained, and reruns do not zero a summary whose raw logs were already purged.

Visitor trends show 14 local calendar days, zero-filled. Device percentages refer to today's page views. Top pages use exact grouped counts over the last 14 days. Total page views combine archived summaries with unarchived events without counting both. Reply rate is replied contacts / non-deleted contacts. Appointment ratio is today's created requests / today's approximate unique visitors; it can exceed 100% and does not claim person-level attribution. No historical visits are fabricated before tracking begins.

Both Admin and Staff use the same AdminDashboard view and charts. Numeric fallback details remain usable without Chart.js. The privacy and personal-data notice pages describe this collection.

## Verification

Release solution builds and relevant tests were run before each of the five commits. Tests cover schema uniqueness/retention, SQL Server snapshot consistency, manager/customer authorization, CSRF, immutable recipients, duplicate submit handling, queue failure recovery, SMTP retry/success marking, encoded email/history content, bot/path filtering, daily hash rotation, timezone boundaries, idempotent aggregation, retained totals, chart data and failed AJAX submissions. Full .NET and JavaScript suites are run again for the final integration.

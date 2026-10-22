# XSPA password recovery

Routes: GET/POST `/dashboard/sifremi-unuttum` and `/dashboard/sifre-sifirla`. Both mutation forms require antiforgery tokens. Recovery requests and reset attempts are each limited to five per IP per 15-minute process-local window. Valid forgot-password requests have the same generic confirmation for existing, inactive and missing accounts, including SMTP failures; transport timing is not constant-time. SMTP errors log only their type, without address, link or token.

Identity generates protected password reset tokens. Links carry a base64url representation and expire after one hour. Password changes invalidate reuse through the Identity security stamp, and a token cannot reset another user. Reset does not sign the user in or activate inactive accounts. Normal Identity password policy applies, plus a 6–128 character UI/server length bound. Authentication cookies remain subject to the configured security-stamp validation interval.

Mail uses the existing authenticated TLS SMTP transport with the selected sender. It is sent during the request with a 90-second deadline; these sensitive token-bearing messages are not stored in Hangfire arguments. There is no automatic delivery retry. The user can request a fresh link after fixing SMTP. This does not change the delivery/recovery behavior of contact replies.

`Authentication:PublicOrigin` optionally sets the canonical HTTPS origin for links; default is `https://xspawellness.runasp.net`. Request Host headers are never used for the reset destination. Keep `DataProtection:KeysPath` configured to a persistent private directory outside wwwroot; otherwise restarting a host with ephemeral keys invalidates issued links and saved SMTP credentials. No database migration is required.

Google auto-linking now explicitly trusts Google's verified email assertion for matching local accounts, including manager accounts, as requested. Email alone is insufficient: the trusted verification claim must be true. Account status, lockout and Identity two-factor checks remain enforced; no roles are added or changed. New users still complete the telephone and privacy-notice registration form.

Recovery views use the shared login layout, semantic labels, validation announcements, new-password autocomplete, keyboard focus styling and the existing reduced-motion support. Reset pages disable response caching and send Referrer-Policy: no-referrer.

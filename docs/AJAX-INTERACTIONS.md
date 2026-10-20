# Asynchronous customer interactions

The five requested changes are split into five commits in this order: blog interactions, comments/replies, profile favorites, contact/newsletter, appointments/global request security.

- Favorite/reaction responses contain authoritative counts. A pending request locks the interaction group; failures retain the previous selection and never automatically retry a toggle.
- Comment mutations return a DTO. The browser refreshes only the Razor-rendered comment fragment, including counts and the owner's recent pending comments. Pending comments remain private until moderation. Newly inserted controls use delegated listeners.
- Profile removal has an explicit remove-only intent, so retrying a removal never adds the favorite back. Other customers' favorites are untouched. Empty paginated views distinguish an empty page from an empty collection.
- Contact and newsletter success follows durable persistence. Validation, CAPTCHA, cooldown, CSRF and background delivery protections remain in place. Failed submissions retain entered values.
- Appointment cancellation uses the existing explicit confirmation panel. Both cancellation and rescheduling preserve owner checks, future-date restrictions and optimistic concurrency. Successful edits return a fresh version for the next save.

`_AjaxSecurity` adds the configured ASP.NET Core CSRF header name/token and `xspa-ajax.js` to the public, login and admin layouts. The fetch interceptor adds AJAX/CSRF headers only for same-origin requests, preserves the form token, and prevents mutation redirects from forwarding it. The form helper rejects external destinations. It never retries a mutation. A 401 opens one shared sign-in prompt, retaining a local return URL; 403 is not treated as a sign-in request. Ordinary HTML form submissions still work without JavaScript.

Validation: Release solution builds after each step with zero warnings/errors; targeted server tests after each change; final complete .NET suite and Node tests, including request-token isolation, failed responses, session expiry, ownership and stale appointment updates. Manual local browser checks cover reaction counts, favorite removal/empty state, comment creation/edit/deletion and dynamically inserted edit toggles. No live customer records are used for these checks.

# Customer appointment management

- Navbar reads the current Identity user's first and last name, with a generic account label for unnamed accounts. It never uses email as the visible account label.
- `/randevularim` lists only the current active member's reservations, with ten records per page. The profile and account menu link to it.
- `/randevularim/{id}/duzenle` supports changing an upcoming booking's published service, date, available time, guest count and notes. Reference, owner and contact identity remain unchanged. Updated confirmed bookings return to Pending.
- `/randevularim/{id}/iptal` requires POST, CSRF and an explicit in-page cancellation confirmation. Past and cancelled bookings are closed to changes.
- Ownership uses existing `CreatedBy`: AppDbContext populates it from the authenticated session and prevents modifications. Matching submitted emails never assigns ownership. Existing signed-in bookings therefore appear without copying private customer data or adding a database column. Anonymous bookings require assistance using their reference code.
- `UpdatedDate` is now an optimistic concurrency token for appointments, together with the existing Status concurrency check. The model snapshot was updated; no SQL schema migration is necessary.
- Creation rejects inactive authenticated members and cross-account replay tokens. Customer queries and mutations independently check the active member and ownership.
- Manager notifications follow customer updates/cancellations. Reconfirmation reuses the unique durable confirmation outbox record, avoiding a duplicate key error.
- The reservation modal prefills the signed-in user's name, email and phone, offers guests a sign-in link, and links signed-in users to their appointments after success.

## Verification

`MyAppointmentEndpointTests` covers ownership isolation (including forged fields), guest email non-claiming, CSRF, audit preservation, edit/cancel lifecycle, stale edits, invalid dates, past/cancelled restrictions, reconfirmation outbox reuse and navbar display names. Existing navbar policy/logout tests now expect names instead of email.

Browser checks use the local development customer and do not change existing reservations. The list and edit form were checked at desktop and 390px mobile widths, including horizontal overflow and automatic field values. Production checks must use the deployed routes, not localhost.

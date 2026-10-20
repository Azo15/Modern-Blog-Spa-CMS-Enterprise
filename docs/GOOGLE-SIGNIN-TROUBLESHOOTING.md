# Google sign-in troubleshooting

The hosted OAuth token endpoint returned `invalid_client` / `The provided client secret is invalid` on 2026-10-20. This is a server credential configuration issue, not evidence of a redirect URI failure.

Use the Client Secret **value** belonging to the same OAuth Client ID. Google Auth Platform > Clients > XSPA Web Client > Client secrets shows only a masked suffix after creation. If the original full value was not saved, the owner must add a secret and transfer the newly displayed value into MonsterASP environment variable `Authentication__Google__ClientSecret`, then restart the application. Never commit or send the secret in chat. The registered callback is `/signin-google`.

Google remote failures now return to sign-in with a generic message. Provider responses, authorization codes and secrets are not included in that message or redirect URL. Existing-account automatic linking remains disabled, and failed authentication never authenticates a user.

Verification: Release build 0 warnings / 0 errors; 12 Google/registration regression tests passed, including invalid-client and correlation-failure handling. Hosting secret replacement and actual Google sign-in must still be completed by the owner. The code change needs normal Git pull/build/publish deployment.

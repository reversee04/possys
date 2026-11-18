# JWT Expiration and Invalid JWT Troubleshooting

## Short answer

Yes. A JWT can expire. Its expiry is normally recorded in the `exp` claim as a Unix timestamp. After that time, the API can reject it with `401 Unauthorized`, `token expired`, or `invalid JWT`.

A JWT cannot be made permanently valid by editing it, changing the local clock, or changing the expiry claim. The token is digitally signed by the issuer. The permanent fix is to obtain a new token through the API's documented login, refresh, or terminal activation process and then make the application renew and store it correctly.

## Important finding for this repository

The current POS EIS client does **not** send a JWT or an `Authorization: Bearer` header. It sends an API key in the `x-api-key` header:

- `PosSystem/Services/EIS/EISApiClient.cs` reads `EIS.ApiKey` and sends `x-api-key`.
- `PosSystem/SendOutboxWorker.cs` uses the same API-key approach.
- `PosSystem/App.config` contains `EIS.ApiKey`.
- `EIS-Integration-Test-Instructions.md` documents `401` troubleshooting as an API-key or terminal-credential problem.

Therefore, if the error is reported as `invalid JWT`, one of these is likely true:

1. The endpoint or environment expects a JWT, but this client is using the wrong authentication method.
2. A proxy, middleware, or a newer integration branch is adding a JWT that has expired or is malformed.
3. The API key is valid for a different environment, terminal, or endpoint.
4. The terminal activation credential has expired, been revoked, or was not stored/used according to the MRA specification.

Do not assume that replacing the value in `EIS.ApiKey` with a JWT is correct. Confirm the required header and token lifecycle with MRA first.

## How to identify the cause

### 1. Confirm the HTTP response

Record only non-sensitive diagnostics:

- HTTP status code, such as `401` or `403`.
- Endpoint path and test/production environment.
- Response error code and message, excluding tokens and API keys.
- UTC time of the request.
- Application version and terminal identifier, if permitted.

A `401` generally means authentication was missing, malformed, expired, revoked, or issued for another environment. A `403` generally means the credential was accepted but the terminal or account is not authorized for that operation.

### 2. Check the authentication contract

Ask the API provider to confirm all of the following:

- Whether this endpoint requires `x-api-key`, `Authorization: Bearer <JWT>`, or another header.
- Whether the credential is for the sandbox/test or production environment.
- Whether the credential belongs to this terminal and taxpayer/site.
- How long the access token is valid.
- Whether a refresh-token endpoint exists.
- Whether terminal activation or reactivation is required.
- Whether the server requires a specific clock tolerance.

The official MRA documentation is the source of truth for endpoint paths, authorization headers, activation, key handling, and retry rules.

### 3. Check the JWT without exposing it

If the application really receives a JWT, decode only the header and payload locally. Do not paste the full token into a public website or commit it to source control. Inspect:

- `exp`: expiration time.
- `nbf`: time before which the token is invalid.
- `iat`: issue time.
- `iss`: expected issuer.
- `aud`: expected audience.
- `sub` or terminal identifier: expected subject.

Compare `exp`, `nbf`, and `iat` with the machine's UTC time. A valid signature does not make a token valid if it is expired, not-yet-valid, issued by the wrong authority, or intended for another audience.

## Permanent resolution

### If the API uses a JWT

1. Obtain fresh credentials from the MRA/API administrator using the official activation or authentication flow.
2. Implement the documented token request in the EIS authentication service.
3. Store the access token with its expiry time, preferably in protected local storage rather than plain `App.config`.
4. Before each request, refresh the token when it is near expiry. Use a small safety window, such as 1 to 5 minutes, to avoid sending a token that expires during a request.
5. On one `401` caused by expiry, refresh once and retry the request once. Do not retry indefinitely.
6. If refresh fails, mark the outbox item as failed/pending, record a redacted error, and require reauthentication or reactivation.
7. Ensure the machine clock is synchronized with a trusted time source using UTC.
8. Keep separate credentials and base URLs for test and production.

Conceptually, requests should contain:

```http
Authorization: Bearer <access-token>
```

The exact header, refresh endpoint, scopes, and activation sequence must match the provider's specification.

### If the API uses the current API-key flow

1. Ask MRA/API support to issue or reactivate the correct API key for this terminal and environment.
2. Put the replacement key in the deployed application's configuration or secret store, not in source code.
3. Verify that the configured URL is the matching test or production URL.
4. Restart the POS application so the `EISApiClient` and outbox worker reload the key.
5. Call the documented ping endpoint, then submit a controlled test transaction.
6. Check that the response is successful before retrying pending outbox entries.
7. Rotate the exposed key immediately if it has ever been committed, shared in screenshots, or included in logs.

The current code has no JWT refresh mechanism. Adding a JWT to `EIS.ApiKey` will not create a working JWT flow; the client must be changed to send the provider-required header and implement the provider-required renewal process.

## What not to do

- Do not edit the JWT payload or signature.
- Do not set a fake future `exp` value.
- Do not disable TLS or certificate validation.
- Do not log complete JWTs, API keys, refresh tokens, or passwords.
- Do not retry an invalid credential forever.
- Do not use a production token against a test URL, or a test token against production.
- Do not delete sales records to hide an authentication failure; preserve them in the outbox for controlled retry.

## Verification checklist

- [ ] Confirm the required authentication scheme with MRA/API support.
- [ ] Confirm the base URL and environment.
- [ ] Confirm the terminal is activated and not blocked.
- [ ] Confirm the machine's UTC clock is correct.
- [ ] Obtain a fresh API key or token through the official process.
- [ ] Update the deployed configuration securely.
- [ ] Restart the POS application.
- [ ] Test the ping endpoint.
- [ ] Submit one controlled test transaction.
- [ ] Retry pending outbox records only after authentication succeeds.
- [ ] Rotate any credential that was exposed.

## Repository changes required for real JWT support

If MRA confirms that this integration must use JWTs, the implementation should be updated in at least these areas:

- `EISApiClient`: add token acquisition, expiry tracking, bearer-header handling, and one-time refresh/retry behavior.
- `SendOutboxWorker`: use the shared authentication service instead of adding an API key directly.
- `App.config`: store only non-secret endpoint settings; move secrets to protected configuration or secure storage.
- EIS DTOs/services: implement the exact activation, authentication, refresh, and terminal-configuration contracts from MRA.
- Logging: record status and redacted error details without recording credentials.

Until the provider confirms the required contract, the safest diagnosis is: this repository currently has an API-key integration, not a JWT integration, so an `invalid JWT` message indicates an authentication-contract or environment mismatch rather than a database problem.

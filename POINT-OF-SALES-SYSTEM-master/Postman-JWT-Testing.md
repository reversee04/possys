# Testing a JWT in Postman

This guide explains how to check whether a JWT works when calling an API from Postman.

## Important: check the required authentication method first

The current POS EIS client sends an API key using the `x-api-key` header. It does not currently send a JWT using `Authorization: Bearer`.

Before testing, confirm from the API provider whether the endpoint requires:

- `Authorization: Bearer <JWT>`
- `x-api-key: <API key>`
- A terminal token or another authentication header

Do not put a JWT into `EIS.ApiKey` unless the provider explicitly says that the value belongs in the `x-api-key` header.

## 1. Create a Postman request

1. Open Postman and create a new HTTP request.
2. Select the HTTP method required by the API, such as `GET` or `POST`.
3. Enter the correct test or production URL.
4. Use the environment that issued the token. A test token normally cannot be used against production.

For this project, the documented health-check endpoint is:

```text
POST https://eis-api.mra.mw/api/v1/utilities/ping
```

Use the official MRA URL and endpoint supplied for your environment if it differs.

## 2. Add the JWT in Postman

### Recommended method: Authorization tab

1. Open the request's **Authorization** tab.
2. Set **Type** to **Bearer Token**.
3. Paste the JWT into the **Token** field.
4. Send the request.

Postman sends this header:

```http
Authorization: Bearer eyJ...
```

### Manual method: Headers tab

You can add the header manually instead:

| Key             | Value                  |
| --------------- | ---------------------- |
| `Authorization` | `Bearer {{jwt_token}}` |

Using a Postman variable such as `{{jwt_token}}` avoids repeatedly pasting the token. Do not commit an exported collection containing a real token.

Do not add both a JWT and an API key unless the API documentation requires both. Extra or conflicting authentication headers can produce misleading failures.

## 3. Send a safe test request

Start with a health-check or read-only endpoint. Avoid submitting a real sale while only testing authentication.

For a `POST` request with no body, choose **Body > none**. If the endpoint requires JSON, choose **Body > raw > JSON** and provide the exact request body required by the API documentation.

Also check that Postman is not sending an old token through a collection, folder, or inherited authorization setting:

1. Open the request's **Headers** tab.
2. Confirm there is exactly one effective `Authorization` header.
3. Check the Postman Console if the request does not behave as expected.

## 4. Interpret the response

| Result                                        | Meaning                                                                                                        |
| --------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| `200 OK` or another documented success status | The token was accepted for this endpoint, environment, and operation.                                          |
| `401 Unauthorized`                            | The token is missing, malformed, expired, revoked, signed by an untrusted issuer, or sent in the wrong header. |
| `403 Forbidden`                               | The token may be valid, but its user, terminal, scope, or role is not allowed to perform this operation.       |
| `404 Not Found`                               | The URL or endpoint may be wrong; this does not by itself prove the JWT is invalid.                            |
| `400 Bad Request`                             | Authentication may have succeeded, but the request body or required fields are invalid.                        |
| `5xx` response                                | The server failed or is unavailable; do not treat this alone as proof of a bad token.                          |

A `200` response from a public endpoint is not proof that the token has permission for sales or other protected operations. Test the specific protected endpoint using a controlled request.

## 5. Check JWT dates locally

A JWT normally has three dot-separated parts:

```text
header.payload.signature
```

In Postman, use the **JWT Decoder** feature if available, or decode only the header and payload in a trusted local tool. Never upload a production token to a public decoder.

Check these claims against the current UTC time:

| Claim | Purpose                                              |
| ----- | ---------------------------------------------------- |
| `exp` | The token expires at this Unix timestamp.            |
| `nbf` | The token cannot be used before this Unix timestamp. |
| `iat` | The time the token was issued.                       |
| `iss` | Must match the expected token issuer.                |
| `aud` | Must match the API's expected audience.              |

The token can still fail even when `exp` is in the future if the issuer, audience, signature, scope, or terminal association is wrong.

## 6. Add a Postman test script

On the request's **Tests** tab, add this script to record whether the response was authenticated successfully:

```javascript
pm.test("Authentication request succeeded", function () {
  pm.expect(pm.response.code).to.be.oneOf([200, 201, 204]);
});

pm.test("Authentication was not rejected", function () {
  pm.expect(pm.response.code).to.not.equal(401);
});

if (pm.response.code === 401) {
  console.warn(
    "The API rejected the credential. Check the token, header, environment, issuer, audience, and expiry.",
  );
}
```

This test checks the HTTP result only. It does not validate the JWT signature inside Postman.

## 7. Test an expired token safely

To confirm that the API enforces expiry:

1. Use a test token issued for the test environment.
2. Send the request before expiry and record the successful result.
3. Wait until the token expires, or use a provider-approved short-lived test token.
4. Send the same request again.
5. Confirm that the API returns `401` and an expiry-related error.

Do not edit the `exp` claim or signature. Editing a signed token makes it invalid; it does not extend its life.

## 8. Permanent resolution for an invalid or expired JWT

There is no permanent client-side way to make an expired JWT valid. The correct solution is:

1. Obtain a new token through the official login, refresh, terminal activation, or reactivation flow.
2. Confirm that the new token is for the same environment, terminal, taxpayer, issuer, and audience.
3. Store it securely and track its expiry time.
4. Refresh it before expiry when the API provides a refresh-token flow.
5. On one expiry-related `401`, refresh once and retry once. Do not retry forever.
6. If refresh fails, require reauthentication or terminal reactivation.
7. Keep the computer clock synchronized to UTC.

For this repository specifically, first confirm whether MRA expects the current `x-api-key` flow or a JWT flow. If it expects an API key, request a replacement or reactivated API key and update the deployed configuration securely. A JWT test in Postman will not make the current C# client use JWT authentication.

## Security checklist

- [ ] Use a test token for testing.
- [ ] Never share a full JWT, API key, or refresh token in screenshots or logs.
- [ ] Use HTTPS.
- [ ] Confirm the environment before sending the request.
- [ ] Check the exact authentication header required by the API.
- [ ] Check `exp`, `nbf`, `iss`, and `aud` where applicable.
- [ ] Rotate any credential that was committed to source control or exposed publicly.
- [ ] Remove real tokens before exporting or sharing a Postman collection.

## Related project documentation

- [JWT-Troubleshooting.md](JWT-Troubleshooting.md)
- [EIS-Integration-Test-Instructions.md](EIS-Integration-Test-Instructions.md)
- [MRA_EIS_API_Integration_Guide.txt](MRA_EIS_API_Integration_Guide.txt)

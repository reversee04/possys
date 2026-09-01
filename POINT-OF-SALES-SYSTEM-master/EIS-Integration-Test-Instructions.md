# EIS Integration: Run & Presentation Test Instructions

This document describes how to verify the EIS integration locally and how to demonstrate it for a presentation.

IMPORTANT: The live MRA endpoints require proper credentials and an activated terminal. For safe demos use the MRA sandbox/test credentials or a local mock server.

1. Quick connectivity check (already attempted)

- A POST to `https://eis-api.mra.mw/api/v1/utilities/ping` returned HTTP 401 Unauthorized in this environment, so the API requires authentication. Ensure you have a valid `x-api-key` or terminal credentials before running.

2. Configure the app for testing

- Open [PosSystem/App.config](PosSystem/App.config) and set:
  - `EIS.BaseUrl` to the MRA test base URL (or your mock server URL).
  - `EIS.ApiKey` to the test API key provided by MRA (or leave blank for mock).
  - `EIS.Enabled` to `true`.

3. Prepare the database (run these in SSMS against `PosDB`)

- Ensure the outbox and support tables exist (from [SQL-Server-Setup.md](SQL-Server-Setup.md)).

  -- View pending outbox entries
  SELECT id, transno, attempts, status, created_at FROM EIS_Outbox ORDER BY created_at DESC;

  -- View EIS transaction history
  SELECT id, transno, submitted_at, status, mra_reference, error_message FROM EISTransaction ORDER BY submitted_at DESC;

  -- View operation logs
  SELECT TOP 100 \* FROM EISLogs ORDER BY logged_at DESC;

4. Run the POS application (Windows / Visual Studio)

- Open `PosSystem.sln` in Visual Studio (Windows).
- Build and run the application.
- In the POS UI, perform a normal sale and pay to complete the checkout.

Expected behavior during checkout (with `EIS.Enabled=true`):

- App marks `tblCart1` rows as `Sold`.
- App attempts synchronous submission to MRA:
  - If submission succeeds, an `EISTransaction` row is inserted containing MRA reference and `qr_data`.
  - If submission fails or times out, the sale payload is inserted into `EIS_Outbox` for background delivery.

5. Manually run the outbox worker (for presentation)

- Option A (recommended for demo): run a small console runner that calls `PosSystem.Services.EIS.SendOutboxWorker.ProcessOutboxAsync()` or reuse the `SendOutboxWorker` from the app.
- Option B: open a SQL client and set an outbox entry to `Pending` then trigger the worker in the app if you added a UI button.

6. Demo checklist (presentation script)

- Show `App.config` with `EIS.Enabled=true` and `EIS.BaseUrl` set to test endpoint.
- Create a test sale in the UI and complete payment.
- Switch to SSMS and refresh `EISTransaction` and `EIS_Outbox`:
  - If online: show `EISTransaction` row with MRA reference and QR.
  - If offline/failure: show `EIS_Outbox` entry and then run worker to resend.
- Use Postman to simulate MRA responses if you are running a local mock server. Import the swagger JSON `https://eis-api.mra.mw/swagger/v1/swagger.json` into Postman and mock `POST /api/v1/sales/submit-sales-transaction` to return a successful payload for the demo.

7. Using Postman to test endpoints (example)

- Import `https://eis-api.mra.mw/swagger/v1/swagger.json` into Postman.
- Create a POST to `/api/v1/sales/submit-sales-transaction` with the `x-api-key` header if required.
- Example body (adjust to exact schema):

```
{
  "invoiceNumber": "T20260825-1001",
  "transactionDate": "2026-08-25T10:00:00",
  "tin": "",
  "totalAmount": 123.45,
  "items": [ { "productCode":"P001","description":"Test","quantity":1,"unitPrice":123.45,"taxAmount":0 } ]
}
```

8. Troubleshooting tips

- If you get 401, confirm `x-api-key` or terminal token present and correct.
- If you get 4xx validation errors, import swagger JSON and validate your JSON payload against the schema.
- If worker doesn't send, check network access from the machine running the app and ensure `EIS.BaseUrl` is reachable.

9. Cleaning up after demo

- If you used a mock server, reset its stored requests.
- If you used live MRA sandbox, remove test invoices if the API supports deletion or notify stakeholders.

10. Advanced: generate QR on receipt

- If the MRA response includes `qrData` or validation signature, display/print it on the receipt. The `EISTransaction.qr_data` column stores this.

If you want, I can also:

- Add a small console runner `Tools/RunOutboxWorker.cs` to make it easier to run the worker during demos.
- Add a simple admin form that lists `EIS_Outbox` and allows manual retry.

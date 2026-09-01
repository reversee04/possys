# EIS Integration Guide for Point-Of-Sales System

This document explains how to integrate an EIS (Enterprise/External Information System) with the existing POS application in this repository (PosSystem). It covers architecture, database changes, C# examples, UI suggestions, testing, and troubleshooting.

## Goals
- Deliver sales/transaction data from the POS to the EIS reliably.
- Track delivery status and responses from EIS.
- Keep minimal disruption to existing POS flows.

## Overview / Architecture
- POS (desktop app) records sales in `tblCart1` and related tables.
- EIS accepts invoices/transactions via a secure HTTP API (JSON) or database link.
- Integration options:
  - Direct HTTP API: POS sends JSON to EIS endpoint (preferred for cloud EIS).
  - Database sync: POS writes to an intermediate `EIS_Outbox` table that the EIS polls.

Diagram (conceptual):

POS -> (on sale complete) -> EIS_Outbox table / HTTP -> EIS API

## Prerequisites
- EIS API endpoint URL, authentication method (API key, client cert, OAuth).
- `PosDB` created and POS SQL objects present.
- Developer access to POS source (this repo) and ability to update `PosSystem` code.

## Recommended DB changes (examples)
Add an outbox and status tracking to avoid losing messages and enable retries.

```sql
-- Outbox table for reliable delivery
CREATE TABLE EIS_Outbox (
    id INT IDENTITY(1,1) PRIMARY KEY,
    transno VARCHAR(100) NOT NULL,
    payload NVARCHAR(MAX) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    attempts INT NOT NULL DEFAULT 0,
    last_attempt DATETIME NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Sent, Failed
    response NVARCHAR(MAX) NULL
);

-- Optional: add delivery columns to cart table
ALTER TABLE tblCart1
ADD eis_sent BIT NOT NULL DEFAULT 0,
    eis_status VARCHAR(50) NULL,
    eis_response NVARCHAR(500) NULL;
```

Seed example (not required if using outbox only):

```sql
INSERT INTO EIS_Outbox (transno, payload)
VALUES ('T12345', '{"example":"payload"}');
```

## Data mapping
Map fields from POS to the EIS invoice model. Example mapping:
- transno -> invoice_number
- sdate -> invoice_date
- cashier -> cashier or user id
- pcode, pdesc, qty, price, total -> invoice line items
- store details (from `tblStore`) -> issuer/store

## C# integration patterns (examples)
Two recommended approaches:
1) Fire-and-forget background worker that reads new outbox rows and POSTs to EIS.
2) Synchronous send at transaction completion, with retry on failure (less resilient).

Below is a simple reliable worker example that uses the `EIS_Outbox` table.

```csharp
// Example: SendOutboxWorker.cs (conceptual)
using System;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class EISClient
{
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _connString;

    public EISClient(string apiUrl, string apiKey, string connString)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _connString = connString;
    }

    public async Task ProcessOutboxAsync()
    {
        using (var cn = new SqlConnection(_connString))
        {
            await cn.OpenAsync();
            var cmd = new SqlCommand("SELECT TOP 10 id, transno, payload, attempts FROM EIS_Outbox WHERE status = 'Pending' ORDER BY created_at", cn);
            using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    int id = r.GetInt32(0);
                    string payload = r.GetString(2);
                    await SendAndMarkAsync(id, payload);
                }
            }
        }
    }

    private async Task SendAndMarkAsync(int id, string payload)
    {
        using (var http = new HttpClient())
        {
            http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            try
            {
                var resp = await http.PostAsync(_apiUrl, content);
                var respText = await resp.Content.ReadAsStringAsync();
                using (var cn = new SqlConnection(_connString))
                {
                    await cn.OpenAsync();
                    var update = new SqlCommand("UPDATE EIS_Outbox SET attempts = attempts + 1, last_attempt = GETDATE(), status = @status, response = @resp WHERE id = @id", cn);
                    update.Parameters.AddWithValue("@id", id);
                    update.Parameters.AddWithValue("@resp", respText ?? "");
                    update.Parameters.AddWithValue("@status", resp.IsSuccessStatusCode ? "Sent" : "Failed");
                    await update.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                using (var cn = new SqlConnection(_connString))
                {
                    await cn.OpenAsync();
                    var update = new SqlCommand("UPDATE EIS_Outbox SET attempts = attempts + 1, last_attempt = GETDATE(), status = 'Failed', response = @resp WHERE id = @id", cn);
                    update.Parameters.AddWithValue("@id", id);
                    update.Parameters.AddWithValue("@resp", ex.ToString());
                    await update.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
```

Integration trigger (example when a sale is completed):
- Build JSON payload from the transaction rows (select from `tblCart1` by `transno`).
- Insert payload into `EIS_Outbox` with status `Pending`.

Example snippet adding to outbox (synchronous insert):

```csharp
string payload = BuildInvoiceJson(transno);
using (var cn = new SqlConnection(DBConnection.MyConnection()))
{
    cn.Open();
    var cm = new SqlCommand("INSERT INTO EIS_Outbox (transno, payload) VALUES (@transno, @payload)", cn);
    cm.Parameters.AddWithValue("@transno", transno);
    cm.Parameters.AddWithValue("@payload", payload);
    cm.ExecuteNonQuery();
}
```

Note: `DBConnection.MyConnection()` in this repo currently returns a connection string. If `MyConnection()` is an instance method, use the appropriate call or fetch the connection string from `App.config`.

## Configuration
- Add EIS settings to `App.config` or a new config section:

```xml
<appSettings>
  <add key="EIS.ApiUrl" value="https://api.example-eis.com/invoices" />
  <add key="EIS.ApiKey" value="DBYX-YM1C-ZJ27-XNDU" />
  <add key="EIS.Enabled" value="true" />
</appSettings>
```

- In code, read settings via `ConfigurationManager.AppSettings["EIS.ApiUrl"]`.

## UI / UX suggestions
- Add a setting screen (e.g., `frmSettings`) to configure EIS URL and API key.
- Add a manual "Resend to EIS" list for failed outbox entries.
- Optionally show `eis_status` per transaction in `frmRecords` or `frmPrint`.

## Security
- Use HTTPS and validate TLS certs.
- Prefer API keys or OAuth; never store plain secrets in source. Use `App.config` or secure vault.
- Log responses but avoid logging full secrets.

## Testing
1. Create a mock EIS endpoint (e.g., Postman mock server or local endpoint) to verify payloads.
2. Insert a test row into `EIS_Outbox` and run the worker.
3. Verify the `EIS_Outbox.status` and `response` fields.
4. Test edge cases: network failure, 500 errors, invalid payloads.

## Troubleshooting
- If you see `invalid object name 'EIS_Outbox'`, confirm:
  - You created the table in the correct database (`PosDB`).
  - `DBConnection.MyConnection()` points to `PosDB` (see `PosSystem/DBConnection.cs`).
- Add logging around HTTP calls and DB writes.

## Deployment checklist
- Update `App.config` with production `EIS.ApiUrl` and secure key.
- Ensure the worker runs (e.g., start with the app or a Windows Service / scheduled task).
- Monitor `EIS_Outbox` for failed entries.

## Rollback
- If EIS integration causes issues, you can disable it by setting `EIS.Enabled=false` in `App.config` and leaving outbox processing paused.

## Example: BuildInvoiceJson (pseudo)
```
{"invoice_number":"T12345","date":"2026-08-13T10:00:00","cashier":"user1","store":{"name":"My Store"},"lines":[{"pcode":"P001","desc":"Product 1","qty":2,"price":100.00}]} 
```

## Final checklist
- [ ] Confirm EIS API details and auth method.
- [ ] Add `EIS_Outbox` table and outbox columns.
- [ ] Implement payload builder and outbox insert on sale completion.
- [ ] Implement/enable worker to deliver outbox messages.
- [ ] Add UI for settings and retry handling.
- [ ] Test with mock and staging EIS.

---

If you want, I can:
- Add the SQL snippets directly into `SQL-Server-Setup.md` (as a patch), or
- Implement a minimal `SendOutboxWorker.cs` file in `PosSystem` with a basic UI hook.

Tell me which next step you prefer.
# MRA EIS Integration Plan for Point-Of-Sales System

This document is a concise, actionable integration plan to connect the existing POS application to the MRA EIS API. Follow the phases below and update the TODOs in the project board as you complete each step.

**Assumptions:**

- The official MRA EIS API specification (endpoints, auth, payloads) will be obtained and used as the source of truth.
- The app will use the existing SQL Server `PosDB` database.
- Secrets (keys/certs) will be stored in `App.config` for initial testing then migrated to a secure store for production.

**Scope:**

- Implement online sales submission to MRA with local persistence and response storage.
- Add onboarding/activation flow for terminals.
- Provide offline queue + reconciliation for certified terminals (phase 2).

**Quick references**

- Existing outbox worker: [PosSystem/SendOutboxWorker.cs](PosSystem/SendOutboxWorker.cs)
- Config: [PosSystem/App.config](PosSystem/App.config)
- DB setup reference: [SQL-Server-Setup.md](SQL-Server-Setup.md)
- MRA guidance: [MRA_EIS_API_Integration_Guide.txt](MRA_EIS_API_Integration_Guide.txt)

---

**Phases & Deliverables**

1. Foundation (Deliverables: `EISApiClient`, config, logging)

- Define `EIS` settings in `App.config` (base URL, env, keys). See [PosSystem/App.config](PosSystem/App.config).
- Create `Services/EIS/EISApiClient.cs` that wraps `HttpClient`, auth header handling, and common error handling.
- Create DTOs for requests/responses under `DTOs/EIS/`.
- Add logging table `EISLogs` and basic logger service.

2. Terminal Onboarding (Deliverables: activation UI + persistence)

- Add Terminal Activation UI (TAC entry) and `TerminalActivationService`.
- Persist terminal configuration locally (new tables or EF model).
- Send activation confirmation to MRA only after local save succeeds.

3. Online Sales Submission (Deliverables: sale mapping and submission)

- Map local sale model to MRA `SaleRequest` DTO.
- On payment completion: save sale locally, then submit to MRA synchronously (or queue) and store response in `EISTransaction` table.
- Use `EISApiClient` for submission; persist validation/QR data in DB.

4. Outbox & Background Delivery (Deliverables: robust retry worker)

- Use the `EIS_Outbox` table added earlier for retries (see [SQL-Server-Setup.md](SQL-Server-Setup.md)).
- Improve `SendOutboxWorker` to support batching, exponential backoff, and health checks.
- Add admin UI to view and retry outbox entries.

5. Offline / Reconciliation (Deliverables: offline queue, LastSubmitted endpoints)

- Implement `OfflineTransaction` table and queueing rules per MRA config (limits, eligibility).
- Implement reconciliation using MRA "Last Submitted" endpoints to resolve uncertain transactions after crashes.

6. Testing & Monitoring (Deliverables: test cases + dashboard)

- Create integration tests using a mock MRA endpoint.
- Add dashboard widgets for API health, queue size, last submission.

---

## MRA Swagger (selected endpoints)

Use the official Swagger/OpenAPI to derive exact DTOs, request/response fields, and error handling. The live spec is available at:

- https://eis-api.mra.mw/swagger/v1/swagger.json

Selected endpoints (grouped) you should implement against the spec:

- Configuration
  - POST /api/v1/configuration/get-latest-configs — retrieve latest global/terminal/taxpayer configuration
  - POST /api/v1/configuration/request-new-terminal-token — request a new terminal token

- Onboarding
  - POST /api/v1/onboarding/activate-terminal — activate a terminal (TAC)
  - POST /api/v1/onboarding/terminal-activated-confirmation — confirm activation after local persist

- Sales
  - POST /api/v1/sales/submit-sales-transaction — submit a sales transaction
  - POST /api/v1/sales/get-invoice-by-number — fetch invoice by number
  - POST /api/v1/sales/last-submitted-online-transaction — reconciliation helper for uncertain submissions
  - POST /api/v1/sales/last-submitted-offline-transaction — retrieve last offline-submitted transaction
  - POST /api/v1/sales/process-credit-debit-note — generate credit/debit notes
  - POST /api/v1/sales/cancel-receipt — create void request for an invoice
  - POST /api/v1/sales/get-void-receipts — list void requests

- Utilities
  - POST /api/v1/utilities/ping — health check
  - POST /api/v1/utilities/product-status — check product mapping/status
  - POST /api/v1/utilities/validate-vat5-certificate — VAT5 validation
  - POST /api/v1/utilities/get-terminal-blocking-message — get blocking message
  - POST /api/v1/utilities/check-terminal-unblock-status — check unblock status
  - POST /api/v1/utilities/get-terminal-site-products — get terminal site products
  - POST /api/v1/utilities/taxpayer-initial-inventory-upload — initial inventory upload (batched)

- Stock / Inventory
  - POST /api/v1/stock/transfer-inventory — transfer inventory
  - GET /api/v1/stock/warehouse-inventory — read warehouse inventory
  - POST /api/v1/stock/submit-adjustment — submit inventory adjustments
  - POST /api/v1/stock/getStockAdjustmentReasons — list adjustment reasons

Notes:

- Use the exact request/response schemas from the swagger JSON when creating DTOs under `DTOs/EIS/`.
- Map each service method to a single endpoint (e.g., `EISSalesService.SubmitAsync()` -> `POST /api/v1/sales/submit-sales-transaction`).
- Implement robust error handling for HTTP and MRA business errors (use `ProblemDetails` / `APIError` schema from the spec).

---

**Database changes (summary)**

- `EIS_Outbox` (already added) — queue for JSON payloads and delivery metadata.
- `EISTransaction` — store MRA response, QR data, validation info.
- `EISLogs` — operational logs for troubleshooting.

**Code structure suggestions**

- Services/EIS/
  - `EISApiClient.cs` — HTTP wrapper
  - `EISSalesService.cs` — maps POS sale -> DTO and submits
  - `TerminalActivationService.cs` — onboarding flows
  - `OfflineQueueService.cs` — queue and retry logic
- DTOs/EIS/ — request/response models matching MRA schema

**Security & Secrets**

- Do not commit production keys. Use `App.config` only for development/test values.
- Plan to migrate secrets to OS-protected storage or a vault before production.

**Testing checklist (minimum)**

- Activation with valid/invalid TACs.
- Normal online sale submission, verify QR data persisted.
- Simulate network failure and verify outbox queueing.
- Power-failure scenario simulation and reconciliation.

**Rollout plan (suggested)**

- Stage 1: Development — implement client, activation, single-sale submission to MRA test endpoint.
- Stage 2: Staging — integrate with MRA sandbox, exercise error cases and offline limits.
- Stage 3: Production — enable EIS in `App.config`, monitor for 7 days.

---

**Next immediate actions I can take now**

- Implement `EISApiClient` and DTO skeletons (I can create these files now).
- Or expand `SendOutboxWorker` with backoff and batching.
- Implement `EISApiClient` and DTO skeletons (done).
- Add `TerminalActivationService`, `EISSalesService`, activation UI skeleton, and DB DDL for EIS artifacts (done).
- Next: I can wire `EISSalesService.SubmitSaleAndPersistAsync()` to persist `EISTransaction` and update checkout flow to call it.
- Or implement outbox worker improvements (batching/backoff) next.

Tell me which immediate action to implement first and I will start it and update the TODOs.

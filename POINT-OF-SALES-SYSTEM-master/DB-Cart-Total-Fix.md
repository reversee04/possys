# Fix: "Cannot insert the value NULL into column 'total'" for `tblCart1`

Issue

- The application attempted to insert rows into `tblCart1` without providing a value for the `total` column. The `total` column is declared `DECIMAL` and `NOT NULL`, causing the INSERT to fail.

What I changed

- Updated the code paths that insert or update `tblCart1` so `total` is always calculated and stored:
  - `PosSystem/frmPOS.cs` — insert now sets `total = price * qty`; update increments `qty` and updates `total` accordingly.
  - `PosSystem/frmQty.cs` — same changes for the quantity entry form.

Why this fixes it

- The `total` value reflects line-item total (price \* qty). By computing and inserting it at the time we add/update the cart row, the DB will no longer receive NULL for that column.

Recommended follow-ups

- Include discount (`disc` / `disc_precent`) in the total calculation if discounts are used:
  total = (price \* qty) - disc
- Add a central helper method like `CalculateLineTotal(price, qty, disc)` to avoid duplicated logic.
- Add database constraints or computed columns if you want `total` derived from `price` and `qty` at the DB level.

Build & test

- Rebuild the solution and re-run the app. The previously failing insert should now succeed.

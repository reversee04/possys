# Fix: "Cannot insert the value NULL into column 'qty'" for `tblStockin`

Issue

- When adding a stock-in item the code attempted to INSERT into `tblStockin` without specifying the `qty` column. The table defines `qty INT NOT NULL`, so the INSERT failed with "Cannot insert the value NULL into column 'qty'".

What I changed (quick fix)

- Updated `PosSystem/frmSearchProductStokin.cs` to include `qty` in the INSERT statement and provide a default value of `1`:

- SQL now: `INSERT INTO tblStockin (refno,pcode,sdate,stockinby,vendorid,qty) VALUES (...)`
- The code sets `@qty = 1` when the item is added. This prevents the NULL insert error and lets the UI/save flow continue.

Why this approach

- Minimal, low-risk change to unblock usage and demos.
- The `qty` value can be adjusted later in the `frmStockin` UI before final save.

Recommended long-term fixes (choose one)

1. Prompt the user for quantity at selection time

- Modify `frmSearchProductStokin` to prompt for desired quantity when the cashier adds an item. Use that value for the insert instead of defaulting to 1.

2. Add a default constraint at the database level

- Alter table to set a default, e.g. `ALTER TABLE tblStockin ADD CONSTRAINT DF_tblStockin_qty DEFAULT 0 FOR qty;` — avoids NULL but may hide missing input errors.

3. Validate in the product-add UI

- Prevent adding an item without a positive quantity by validating inputs in the UI and enabling editing in the `frmStockin` grid prior to save.

How to apply the DB default (if you prefer)

```sql
ALTER TABLE tblStockin ADD CONSTRAINT DF_tblStockin_qty DEFAULT 0 FOR qty;
```

If you want, I can implement Option 1 (prompt for qty on add) now — do you want that?

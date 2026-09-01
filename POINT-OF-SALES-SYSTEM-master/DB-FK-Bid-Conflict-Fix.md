 Fix: FOREIGN KEY conflict on `TblProduct1.bid` -> `BrandTbl(id)`

Error seen:

The INSERT statement conflicted with the FOREIGN KEY constraint "FK__TblProduct1__bid__5070F446". The conflict occurred in database "PosDB", table "dbo.BrandTbl", column 'id'. The statement has been terminated.

What this means
- Your `TblProduct1` row is referencing a `bid` value that does not exist in the `BrandTbl` table. The database prevents inserts/updates that would break the referential integrity.

Quick checklist (recommended order)

1. Inspect existing brands:

```sql
USE PosDB;
SELECT id, brand FROM BrandTbl ORDER BY id;
```

2. Find product rows that reference missing brand ids:

```sql
-- shows products whose bid has no matching BrandTbl.id
SELECT p.pcode, p.pdesc, p.bid
FROM TblProduct1 p
LEFT JOIN BrandTbl b ON p.bid = b.id
WHERE b.id IS NULL;
```

3. Fix options (choose one)

- Option A — Create missing Brand rows (keeps existing `bid` values):

  If the missing `bid` values are correct and you want to create brand records with those ids:

  ```sql
  -- Example: create brand with specific id 123 (only use if you understand identity impact)
  SET IDENTITY_INSERT BrandTbl ON;
  INSERT INTO BrandTbl (id, brand) VALUES (123, 'Unknown Brand');
  SET IDENTITY_INSERT BrandTbl OFF;
  ```

  Repeat for each missing id (replace 123 and 'Unknown Brand'). Prefer creating meaningful brand names.

- Option B — Re-point product rows to an existing brand id (recommended when data entry used incorrect bid):

  ```sql
  -- Replace badBid with the bad value, goodBid with an existing BrandTbl.id
  UPDATE TblProduct1
  SET bid = @goodBid
  WHERE bid = @badBid;
  ```

  Example:
  ```sql
  UPDATE TblProduct1 SET bid = 1 WHERE bid = 9999;
  ```

- Option C — Remove or relax the foreign key (NOT RECOMMENDED for production):

  ```sql
  -- Temporarily disable the FK (for bulk import). Re-enable after fix.
  ALTER TABLE TblProduct1 NOCHECK CONSTRAINT FK__TblProduct1__bid__5070F446;

  -- After fixes
  ALTER TABLE TblProduct1 CHECK CONSTRAINT FK__TblProduct1__bid__5070F446;
  ```

  Use this only when you fully understand the risks.

4. Re-run the failing operation (insert/update).

Prevention (recommended)
- Ensure the product creation UI requires selecting an existing brand (or creates the brand first).
- Validate `bid` exists in code before inserting products. Example pseudocode:

```
if (!BrandExists(bid)) { CreateBrand(bid, name); }
InsertProduct(...);
```

- For bulk imports, import/create BrandTbl entries first, then import products.

If you want, I can:
- Add a SQL script to create missing Brand rows for all orphan `bid` values (automated fix), or
- Patch the product-create code (`frmProduct.cs` or related) to validate/create brands before insert.

Tell me which automated fix you prefer and I will apply it.

# SQL Server Setup for Point of Sale System

This guide walks through the SQL Server setup needed to run the `POINT-OF-SALES-SYSTEM-master` desktop application.

## 1. Prerequisites

- Windows OS.
- SQL Server Express (or full SQL Server) installed.
- SQL Server Management Studio (SSMS) installed.
- Visual Studio or another C# IDE to open `PosSystem.sln`.

## 2. SQL Server Instance

The app currently uses this connection string in `PosSystem/DBConnection.cs`:

```csharp
return @"Data Source=.\SQLEXPRESS;Initial Catalog=PosDB;Integrated Security=True";
```

So you should create or use an instance named `SQLEXPRESS`. If you use a different instance, update the connection string in `DBConnection.cs`.

## 3. Create the Database

Open SSMS and connect to your instance. Then execute:

```sql
CREATE DATABASE PosDB;
GO
USE PosDB;
GO
```

## 4. Create Tables

Run the following SQL script in SSMS while `PosDB` is selected:

```sql
CREATE TABLE TblCatecory (
    id INT IDENTITY(1,1) PRIMARY KEY,
    category VARCHAR(100) NOT NULL
);

CREATE TABLE BrandTbl (
    id INT IDENTITY(1,1) PRIMARY KEY,
    brand VARCHAR(100) NOT NULL
);

CREATE TABLE TblProduct1 (
    pcode VARCHAR(50) PRIMARY KEY,
    barcode VARCHAR(100),
    pdesc VARCHAR(250) NOT NULL,
    bid INT NOT NULL,
    cid INT NOT NULL,
    price DECIMAL(18,2) NOT NULL,
    qty INT NOT NULL DEFAULT 0,
    reorder INT NOT NULL DEFAULT 0,
    FOREIGN KEY (bid) REFERENCES BrandTbl(id),
    FOREIGN KEY (cid) REFERENCES TblCatecory(id)
);

CREATE TABLE tblStore (
    id INT IDENTITY(1,1) PRIMARY KEY,
    store VARCHAR(200),
    address VARCHAR(300),
    phone VARCHAR(100)
);

CREATE TABLE tblUser (
    id INT IDENTITY(1,1) PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    password VARCHAR(200) NOT NULL,
    role VARCHAR(50) NOT NULL,
    name VARCHAR(150),
    isactive BIT NOT NULL DEFAULT 1
);

CREATE TABLE tblVendor (
    id INT IDENTITY(1,1) PRIMARY KEY,
    vender VARCHAR(200) NOT NULL,
    address VARCHAR(300),
    contactperson VARCHAR(150),
    telephone VARCHAR(100),
    email VARCHAR(150),
    fax VARCHAR(100)
);

CREATE TABLE tblCart1 (
    id INT IDENTITY(1,1) PRIMARY KEY,
    transno VARCHAR(100) NOT NULL,
    pcode VARCHAR(50) NOT NULL,
    price DECIMAL(18,2) NOT NULL,
    qty INT NOT NULL,
    disc DECIMAL(18,2) NOT NULL DEFAULT 0,
    disc_precent DECIMAL(18,2) NOT NULL DEFAULT 0,
    total DECIMAL(18,2) NOT NULL,
    sdate DATETIME NOT NULL,
    cashier VARCHAR(100),
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    FOREIGN KEY (pcode) REFERENCES TblProduct1(pcode)
);

CREATE TABLE tblStockin (
    id INT IDENTITY(1,1) PRIMARY KEY,
    refno VARCHAR(100) NOT NULL,
    pcode VARCHAR(50) NOT NULL,
    sdate DATETIME NOT NULL,
    stockinby VARCHAR(100),
    vendorid INT,
    qty INT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    FOREIGN KEY (pcode) REFERENCES TblProduct1(pcode),
    FOREIGN KEY (vendorid) REFERENCES tblVendor(id)
);

CREATE TABLE tblCancel (
    id INT IDENTITY(1,1) PRIMARY KEY,
    transno VARCHAR(100),
    pcode VARCHAR(50),
    price DECIMAL(18,2),
    qty INT,
    sdate DATETIME,
    voidby VARCHAR(100),
    cancelledby VARCHAR(100),
    reason VARCHAR(500),
    action VARCHAR(100),
    FOREIGN KEY (pcode) REFERENCES TblProduct1(pcode)
);

CREATE TABLE tblAdjustment (
    id INT IDENTITY(1,1) PRIMARY KEY,
    referenceno VARCHAR(100),
    pcode VARCHAR(50) NOT NULL,
    qty INT NOT NULL,
    action VARCHAR(50) NOT NULL,
    remarks VARCHAR(500),
    sdate DATETIME NOT NULL,
    [user] VARCHAR(100),
    FOREIGN KEY (pcode) REFERENCES TblProduct1(pcode)
);
```

## 5. Create Views

The code uses several views. Use these example definitions:

```sql
CREATE VIEW vwCriticalItems AS
SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.qty, p.reorder
FROM TblProduct1 p
JOIN BrandTbl b ON p.bid = b.id
JOIN TblCatecory c ON p.cid = c.id
WHERE p.qty <= p.reorder;

CREATE VIEW vwSoldItems AS
SELECT c.id, c.transno, c.pcode, p.pdesc, c.price, c.qty, c.disc, c.total, c.sdate, c.cashier, c.status
FROM tblCart1 c
JOIN TblProduct1 p ON c.pcode = p.pcode;

CREATE VIEW vwStockin AS
SELECT s.id, s.refno, s.pcode, p.pdesc, s.qty, s.sdate, s.stockinby, s.status, v.vender
FROM tblStockin s
JOIN TblProduct1 p ON s.pcode = p.pcode
LEFT JOIN tblVendor v ON s.vendorid = v.id;

CREATE VIEW vwCancelledOrder AS
SELECT c.id, c.transno, c.pcode, p.pdesc, c.price, c.qty, c.sdate, c.voidby, c.cancelledby, c.reason, c.action
FROM tblCancel c
JOIN TblProduct1 p ON c.pcode = p.pcode;
```

## 6. Seed Initial Data

Create at least one admin user and a store record:

```sql
INSERT INTO tblStore (store, address, phone)
VALUES ('My Store', '123 Main Street', '123-456-7890');

INSERT INTO tblUser (username, password, role, name, isactive)
VALUES ('admin', 'admin', 'Admin', 'Administrator', 1);
```

> Note: The app stores plain text passwords in the current code. For production, use hashed passwords and secure authentication.

## 7. Update the Connection String

If your instance is not `.\SQLEXPRESS`, open `PosSystem/DBConnection.cs` and update:

```csharp
return @"Data Source=.\SQLEXPRESS;Initial Catalog=PosDB;Integrated Security=True";
```

Examples:

- Local default instance: `Data Source=localhost;Initial Catalog=PosDB;Integrated Security=True`
- Named instance: `Data Source=SERVERNAME\SQLEXPRESS;Initial Catalog=PosDB;Integrated Security=True`

## 8. Run the App

1. Open `PosSystem.sln` in Visual Studio.
2. Build the solution.
3. Run the app and log in with the seeded admin credentials.

## 9. Troubleshooting

- If the app cannot connect, check SQL Server is running and `PosDB` exists.
- If using SQL authentication, switch the connection string to use `User ID=...;Password=...;`.
- If the app fails on missing tables or views, verify the SQL script executed without errors.

## 10. Notes

- This guide is based on the SQL object names referenced in the app source code.
- Some columns may require adjustment if your actual database schema differs.
- Use the SQL above to create a working starting schema for development.

## 11. EIS Outbox (optional)

To support reliable delivery of sales/transactions to an external EIS, create an outbox table and optional tracking columns on `tblCart1`.

Run the following while `PosDB` is selected:

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

-- EIS transaction table to store submission results
CREATE TABLE EISTransaction (
id INT IDENTITY(1,1) PRIMARY KEY,
transno VARCHAR(100) NOT NULL,
submitted_at DATETIME NULL,
status VARCHAR(50) NULL,
mra_reference VARCHAR(200) NULL,
validation_data NVARCHAR(MAX) NULL,
qr_data NVARCHAR(MAX) NULL,
error_code VARCHAR(100) NULL,
error_message NVARCHAR(MAX) NULL
);

-- EIS operational logs
CREATE TABLE EISLogs (
id INT IDENTITY(1,1) PRIMARY KEY,
logged_at DATETIME NOT NULL DEFAULT GETDATE(),
operation VARCHAR(100) NOT NULL,
invoiceNumber VARCHAR(100) NULL,
status VARCHAR(50) NULL,
errorMessage NVARCHAR(MAX) NULL,
durationMs INT NULL
);

-- Offline transactions queue
CREATE TABLE OfflineTransaction (
id INT IDENTITY(1,1) PRIMARY KEY,
transno VARCHAR(100) NOT NULL,
created_at DATETIME NOT NULL DEFAULT GETDATE(),
status VARCHAR(50) NOT NULL DEFAULT 'Pending',
retry_count INT NOT NULL DEFAULT 0,
last_error NVARCHAR(MAX) NULL
);

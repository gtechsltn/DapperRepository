-- Drop old tables if exist (be careful in production!)
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Addresses', 'U') IS NOT NULL DROP TABLE dbo.Addresses;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- Users table
CREATE TABLE dbo.Users
(
    UserId     INT IDENTITY(1,1) PRIMARY KEY,
    Name       NVARCHAR(100) NOT NULL,
    Email      NVARCHAR(100) NULL
);
GO

-- Addresses table (1-to-1 or 1-to-many depending on business rule)
CREATE TABLE dbo.Addresses
(
    AddressId  INT IDENTITY(1,1) PRIMARY KEY,
    UserId     INT NOT NULL,
    City       NVARCHAR(100) NOT NULL,
    Street     NVARCHAR(200) NULL,
    ZipCode    NVARCHAR(20) NULL,
    CONSTRAINT FK_Addresses_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users(UserId)
        ON DELETE CASCADE
);
GO

-- Orders table (many-to-1: many orders per user)
CREATE TABLE dbo.Orders
(
    OrderId         INT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT NOT NULL,
    ProductName     NVARCHAR(200) NULL,
    Quantity        INT NULL,
    Amount          DECIMAL(18,2) NOT NULL,
    OrderDate       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId)
        REFERENCES dbo.Users(UserId)
        ON DELETE CASCADE
);
GO

-- Optional: some sample data
INSERT INTO dbo.Users (Name) VALUES (N'Alice'), (N'Bob');

INSERT INTO dbo.Addresses (UserId, City, Street, ZipCode)
VALUES 
(1, N'Paris', N'123 Rue de Rivoli', N'75001'),
(2, N'London', N'45 Oxford Street', N'W1D 2LT');

INSERT INTO dbo.Orders (UserId, Amount)
VALUES 
(1, 99.50),
(1, 149.99),
(2, 250.00);
GO

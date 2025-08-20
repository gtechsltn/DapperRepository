-- 1. Create Users table if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Email NVARCHAR(255) NOT NULL,
        Name NVARCHAR(255) NOT NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        DeletedAt DATETIME NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- 2. Create UNIQUE filtered index if not exists
IF NOT EXISTS (
    SELECT * 
    FROM sys.indexes 
    WHERE name = 'UQ_Users_Email_NotDeleted' AND object_id = OBJECT_ID('Users')
)
BEGIN
    CREATE UNIQUE INDEX UQ_Users_Email_NotDeleted ON Users (Email)
    WHERE IsDeleted = 0;
END
GO

-- 3. Drop trigger if exists
IF OBJECT_ID('TRG_Users_UpdateTimestamp', 'TR') IS NOT NULL
    DROP TRIGGER TRG_Users_UpdateTimestamp;
GO

-- 4. Create trigger in its own batch
CREATE TRIGGER TRG_Users_UpdateTimestamp ON Users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Users
    SET UpdatedAt = GETUTCDATE()
    FROM Users u
    INNER JOIN inserted i ON u.Id = i.Id;
END
GO

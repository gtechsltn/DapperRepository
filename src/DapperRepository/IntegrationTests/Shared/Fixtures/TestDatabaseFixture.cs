using Microsoft.Data.SqlClient;

namespace DapperRepository.IntegrationTests.Shared.Fixtures;

public class TestDatabaseFixture : IDisposable
{
    public static string MasterConnection { get; }
    public static string DefaultConnection { get; }

    static TestDatabaseFixture()
    {
        // LocalDB test database
        MasterConnection = @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

        // LocalDB test database
        DefaultConnection = @"Server=(localdb)\MSSQLLocalDB;Database=MyDatabaseTest;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

        // Ensure the database is created before any tests run
        using var conn = new SqlConnection(MasterConnection);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'MyDatabaseTest') CREATE DATABASE MyDatabaseTest;";
        cmd.ExecuteNonQuery();
    }

    public TestDatabaseFixture()
    {
        using var conn = new SqlConnection(DefaultConnection);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            IF OBJECT_ID('dbo.Users') IS NULL
            CREATE TABLE Users (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Email NVARCHAR(255) NOT NULL,
                Name NVARCHAR(255) NOT NULL,
                IsDeleted BIT NOT NULL DEFAULT 0,
                DeletedAt DATETIME NULL,
                CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
                UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE()
            );

            DELETE FROM Users;
            DBCC CHECKIDENT ('Users', RESEED, 0);

            IF NOT EXISTS (SELECT 1 FROM Users)
            BEGIN
                INSERT INTO Users (Email, Name, IsDeleted, DeletedAt, CreatedAt, UpdatedAt)
                VALUES
                    ('alice@example.com', 'Alice', 0, NULL, GETUTCDATE(), GETUTCDATE()),
                    ('bob@example.com', 'Bob', 0, NULL, GETUTCDATE(), GETUTCDATE()),
                    ('charlie@example.com', 'Charlie', 0, NULL, GETUTCDATE(), GETUTCDATE()),
                    ('deleted.user@example.com', 'Deleted User', 1, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());
            END
        ";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        using var conn = new SqlConnection(DefaultConnection);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DROP DATABASE MyDatabaseTest;";
        try { cmd.ExecuteNonQuery(); } catch { }
    }
}

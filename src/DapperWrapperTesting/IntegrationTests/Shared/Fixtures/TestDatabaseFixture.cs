using System.Data;

using DapperWrapperTesting.Infrastructure;

using Microsoft.Data.SqlClient;

namespace DapperWrapperTesting.IntegrationTests.Shared.Fixtures;

public class TestDatabaseFixture : IDisposable
{
    public static string MasterConnection { get; }
    public static string DefaultConnection { get; }

    static TestDatabaseFixture()
    {
        // LocalDB test database
        MasterConnection = @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

        // LocalDB test database
        DefaultConnection = @"Server=(localdb)\MSSQLLocalDB;Database=DapperWrapperTestingDb;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;";

        DbHelper.CreateDatabaseAndTables(MasterConnection, DefaultConnection, "Scripts");
    }

    public IDbConnection GetOpenConnection()
    {
        var conn = new SqlConnection(DefaultConnection);
        conn.Open();
        return conn;
    }

    public void Dispose()
    {
        using var conn = new SqlConnection(DefaultConnection);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DROP DATABASE DapperWrapperTestingDb;";
        try { cmd.ExecuteNonQuery(); } catch { }
    }
}

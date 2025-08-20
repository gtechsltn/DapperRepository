/*
dotnet add package Microsoft.Extensions.Configuration --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration.FileExtensions --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration.Json --version 8.0.0
dotnet add package Dapper
dotnet add package Microsoft.Data.SqlClient
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Extensions.Hosting --version 8.0.0
dotnet add package Serilog.Settings.Configuration --version 8.0.0
 */

using Dapper;

using Microsoft.Data.SqlClient;

using Serilog;

namespace DapperWrapperTesting.Infrastructure;

public class DbHelper
{
    public static void CreateDatabaseAndTables(string serverDefaultConnection, string defaultConnection, string scriptsFolder)
    {
        var builder = new SqlConnectionStringBuilder(defaultConnection);
        string databaseName = builder.InitialCatalog; // <-- this is the database name

        Log.Information("Starting database setup...");

        // --- 2. Create database if missing ---
        using (var connection = new SqlConnection(serverDefaultConnection))
        {
            connection.Open();
            string sqlCreateDb = $@"
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}')
BEGIN
    CREATE DATABASE [{databaseName}];
END
";
            connection.Execute(sqlCreateDb);
            Log.Information("Database '{Database}' is ready.", databaseName);
        }

        // --- 3. Execute scripts within transaction ---
        var scriptFiles = Directory.GetFiles(scriptsFolder, "*.sql");
        Array.Sort(scriptFiles); // Ensure scripts run in order

        using (var dbConnection = new SqlConnection(defaultConnection))
        {
            dbConnection.Open();
            using var transaction = dbConnection.BeginTransaction();

            string filePath = string.Empty; // To capture the file path for logging
            try
            {
                foreach (var file in scriptFiles)
                {
                    filePath = file; // Capture the file path for logging
                    string sql = File.ReadAllText(file);

                    // Split into batches by GO
                    var batches = sql.Split(["\r\nGO\r\n", "\nGO\n", "\rGO\r"], StringSplitOptions.RemoveEmptyEntries);

                    foreach (var batch in batches)
                    {
                        if (!string.IsNullOrWhiteSpace(batch))
                        {
                            dbConnection.Execute(batch, transaction: transaction);
                            Log.Information("Executed batch from File: {FilePath}, Script: \n{Batch}", Path.GetFileName(file), batch);
                        }
                    }
                }

                transaction.Commit();
                Log.Information("All scripts executed successfully and transaction committed.");
            }
            catch (SqlException ex)
            {
                Log.Error("SqlException while executing {Script}", Path.GetFileName(filePath));

                for (int i = 0; i < ex.Errors.Count; i++)
                {
                    var error = ex.Errors[i];
                    Log.Error(
                        "Error {Index}: Number={Number}, Message={Message}, Line={LineNumber}, Procedure={Procedure}",
                        i + 1,
                        error.Number,
                        error.Message,
                        error.LineNumber,
                        error.Procedure
                    );
                }

                transaction.Rollback();
                throw;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "Transaction rolled back due to error.");
                throw;
            }
        }
    }
}
using DapperWrapperTesting.Infrastructure;

using Microsoft.Extensions.Configuration;

using Serilog;

namespace DapperWrapperTesting;

public class Program
{
    public static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        var masterConnection = configuration.GetConnectionString("MasterConnection");
        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        var scriptsFolder = configuration["Scripts:Folder"];

        Log.Information("MasterConnection connection: {Conn}", masterConnection);
        Log.Information("DefaultConnection connection: {Conn}", defaultConnection);
        Log.Information("Scripts folder: {Folder}", scriptsFolder);

        try
        {
            Log.Information("Application Starting...");

            Log.Debug("This is a debug message.");
            Log.Warning("This is a warning message.");
            Log.Error("This is an error message.");

            Log.Information("Hello, Serilog!");

            if (string.IsNullOrWhiteSpace(masterConnection))
            {
                throw new ArgumentNullException(nameof(masterConnection), "MasterConnection connection string cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(defaultConnection))
            {
                throw new ArgumentNullException(nameof(defaultConnection), "DefaultConnection connection string cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(scriptsFolder))
            {
                throw new ArgumentNullException(nameof(scriptsFolder), "Scripts folder path cannot be null or empty.");
            }

            DbHelper.CreateDatabaseAndTables(masterConnection, defaultConnection, scriptsFolder);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly!");
        }
        finally
        {
            Log.CloseAndFlush(); // Ensure logs are written
        }
    }
}
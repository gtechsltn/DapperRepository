/*
dotnet add package Dapper
dotnet add package Microsoft.Data.SqlClient

dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Extensions.Hosting --version 8.0.0
dotnet add package Serilog.Settings.Configuration --version 8.0.0

dotnet add package Microsoft.Extensions.Configuration --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration.FileExtensions --version 8.0.0
dotnet add package Microsoft.Extensions.Configuration.Json --version 8.0.0
 */

using DapperRepository.Application.Dtos;
using DapperRepository.Domain.Entities;
using DapperRepository.Infrastructure;
using DapperRepository.Infrastructure.Repositories;

using Microsoft.Extensions.Configuration;

using Serilog;

namespace DapperRepository;

public class Program
{
    public static void Main(string[] args)
    {
        /*
        // --- Configure Serilog from C# ---
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()                 // Minimum log level
            .WriteTo.Console()                    // Output logs to console
            .WriteTo.File("logs/log-.txt",        // Output logs to rolling file
                          rollingInterval: RollingInterval.Day,
                          retainedFileCountLimit: 7,
                          encoding: Encoding.UTF8,
                          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        */

        var configuration = new ConfigurationBuilder()
               .SetBasePath(AppContext.BaseDirectory)
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .Build();

        // Configure Serilog from appsettings.json
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

            // Example logs
            Log.Debug("This is a debug message.");
            Log.Warning("This is a warning message.");
            Log.Error("This is an error message.");

            Log.Information("Hello, Serilog!");

            // Validate configuration values to ensure they are not null or empty
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

            // Call the method after validation
            DbHelper.CreateDatabaseAndTables(masterConnection, defaultConnection, scriptsFolder);

            UsageExamples(configuration);
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

    private static void UsageExamples(IConfiguration configuration)
    {
        PagedResult<User> result = new PagedResult<User>();

        var repo = new UserRepository(new SqlConnectionFactory(configuration), new DapperWrapper());

        // Insert or reactivate
        int userId = repo.Upsert(new User { Email = "alice@example.com", Name = "Alice" });

        // Soft-delete
        repo.SoftDelete(userId);

        // Get all active users
        var users = repo.GetAll();

        // Get user by Id
        var user = repo.GetById(userId);

        // Search with paging
        result = repo.Search(
            searchTerm: "alice",
            sortColumn: "Name",
            sortDescending: false,
            page: -1,
            pageSize: -1
        );

        Log.Information($"Total Users Found: {result.TotalCount}");
        foreach (var u in result.Items)
        {
            Log.Information($"{u.Id}: {u.Email} - {u.Name}");
        }

        int existingUserId = userId;

        // Soft-delete existing user
        repo.SoftDelete(existingUserId);

        // Upsert/reactivate => Throws exception if user existed and is soft-deleted
        var upsertUserId = repo.Upsert(new User { Email = "abc@example.com", Name = "Alice Updated" });

        // Insert brand new user
        var newUserId = repo.Upsert(new User { Email = "new@example.com", Name = "Bob" });

        // Insert or reactivate
        int newUpsertUserId = repo.Upsert(new User { Email = "alice@example.com", Name = "Alice" });

        // Search with paging
        result = repo.Search(
            searchTerm: "alice", // --> Ignore case sensitivity in SQL Server is default for string comparisons
            sortColumn: "Name",
            sortDescending: false,
            page: 1,
            pageSize: 10
        );

        Log.Information($"Total Users Found: {result.TotalCount}");
        foreach (var u in result.Items)
        {
            Log.Information($"{u.Id}: {u.Email} - {u.Name}");
        }
    }
}
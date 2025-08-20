# Dapper Repository

Moq.Dapper helper extension

https://github.com/gtechsltn/DapperRepository

https://chatgpt.com/c/68a52a1c-b6c8-8331-826d-6d6bae6a24aa

C# Dapper-friendly implementation with soft-delete methods that respects this unique constraint automatically.

**This repository provides a complete solution for managing user data with soft-delete functionality, including advanced search capabilities, dynamic sorting, and pagination.**

+ .NET 8.0
+ Console App (C#)
+ SQL Server
+ SQL Server Management Studio (SSMS)
+ Dapper for database operations
+ Serilog for logging
+ xUnit for unit tests
+ Moq.Dapper for mocking Dapper calls
+ xUnit for integration tests

# Feature Overview
.NET 8 Console App that does exactly that:
+ Creates the database if missing
+ Executes multiple scripts in order
+ Uses a transaction (rollback if anything fails)
+ Logs progress and errors with Serilog
+ Uses Dapper for script execution

# Features Covered
+ GetAll / GetById → supports filtering by IsDeleted
+ Search / Sorting / Paging → returns TotalCount for UI pagination
+ SoftDelete → marks row deleted instead of physical delete
+ Insert / Update / Upsert → handles soft-deleted users correctly
+ Dapper-friendly parameter handling → avoids SQL injection, supports strong typing

# Key Points
+ Soft-delete: IsDeleted + DeletedAt ensures “deleted” rows remain for audit/history.
+ Filtered unique index: Enforces uniqueness only for active users.
+ Dapper queries: Always filter IsDeleted = 0 for reads and updates.
+ Concurrency-safe inserts: SQL Server enforces uniqueness; Dapper will throw a SQL exception if a non-deleted duplicate exists.

## Add "Upsert or Reactivate" method in the repository. This method will:
+ Check if a user with the same email exists.
+ If it exists and is soft-deleted, reactivate it (clear IsDeleted and DeletedAt, update Name).
+ If it exists and is active, throw an exception or return a conflict.
+ If it doesn't exist, insert a new user.

## Search Method Enhancements

### Normalization step
```
string? normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
```

→ ensures "", " ", or null all behave like null.

### SQL uses @SearchTerm IS NULL

→ If normalizedSearch == null, the filter is skipped.

**Still supports pagination and sorting.**

# SQL Table Definition
+ Soft-delete (IsDeleted + DeletedAt)
+ Unique constraint on a column (Email) that ignores soft-deleted rows
+ Audit columns (CreatedAt, UpdatedAt)
```
CREATE TABLE Users
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE()
);

-- Filtered unique index to enforce uniqueness only for non-deleted users
CREATE UNIQUE INDEX UQ_Users_Email_NotDeleted
ON Users (Email)
WHERE IsDeleted = 0;

-- Optional: trigger to automatically update UpdatedAt on update
CREATE TRIGGER TRG_Users_UpdateTimestamp
ON Users
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Users
    SET UpdatedAt = GETUTCDATE()
    FROM Users u
    INNER JOIN inserted i ON u.Id = i.Id;
END;
```

# NuGet Packages
```
dotnet add package Dapper
dotnet add package Microsoft.Data.SqlClient
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Extensions.Logging
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Serilog
dotnet add package Serilog.Extensions.Logging
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Extensions.Configuration
```

# Dapper Repository
```
using Dapper;
using System.Data;
using System.Data.SqlClient;

public class UserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection Connection => new SqlConnection(_connectionString);

    // Get all users (optional filtering by IsDeleted)
    public IEnumerable<User> GetAll(bool includeDeleted = false)
    {
        string sql = "SELECT * FROM Users WHERE (@IncludeDeleted = 1 OR IsDeleted = 0)";
        using var db = Connection;
        return db.Query<User>(sql, new { IncludeDeleted = includeDeleted ? 1 : 0 });
    }

    // Get by Id (optional filtering by IsDeleted)
    public User? GetById(int id, bool includeDeleted = false)
    {
        string sql = "SELECT * FROM Users WHERE Id = @Id AND (@IncludeDeleted = 1 OR IsDeleted = 0)";
        using var db = Connection;
        return db.QuerySingleOrDefault<User>(sql, new { Id = id, IncludeDeleted = includeDeleted ? 1 : 0 });
    }

    // Search, Sort, and Paging
    public PagedResult<User> Search(
        string? searchTerm = null,
        string? sortColumn = null,
        bool sortDescending = false,
        int page = 1,
        int pageSize = 20,
        bool includeDeleted = false)
    {
        using var db = Connection;
        var sql = @"
            SELECT COUNT(*) FROM Users
            WHERE (@IncludeDeleted = 1 OR IsDeleted = 0)
              AND (@SearchTerm IS NULL OR Email LIKE '%' + @SearchTerm + '%' OR Name LIKE '%' + @SearchTerm + '%');

            SELECT * FROM Users
            WHERE (@IncludeDeleted = 1 OR IsDeleted = 0)
              AND (@SearchTerm IS NULL OR Email LIKE '%' + @SearchTerm + '%' OR Name LIKE '%' + @SearchTerm + '%')
        ";

        // Add ORDER BY and OFFSET/FETCH for paging
        if (!string.IsNullOrWhiteSpace(sortColumn))
        {
            sql += $" ORDER BY {sortColumn} {(sortDescending ? "DESC" : "ASC")}";
        }
        else
        {
            sql += " ORDER BY Id ASC"; // default order
        }

        sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var multi = db.QueryMultiple(sql, new
        {
            IncludeDeleted = includeDeleted ? 1 : 0,
            SearchTerm = searchTerm,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        var totalCount = multi.ReadSingle<int>();
        var items = multi.Read<User>();

        return new PagedResult<User>
        {
            TotalCount = totalCount,
            Items = items
        };
    }

    // Soft-delete user
    public void SoftDelete(int id)
    {
        const string sql = @"
            UPDATE Users
            SET IsDeleted = 1,
                DeletedAt = GETUTCDATE(),
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id
        ";

        using var db = Connection;
        db.Execute(sql, new { Id = id });
    }

    // Insert user
    public int Insert(User user)
    {
        const string sql = @"
            INSERT INTO Users (Email, Name)
            VALUES (@Email, @Name);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
        ";

        using var db = Connection;
        return db.QuerySingle<int>(sql, user);
    }

    // Update user (only non-deleted)
    public void Update(User user)
    {
        const string sql = @"
            UPDATE Users
            SET Name = @Name,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND IsDeleted = 0
        ";

        using var db = Connection;
        db.Execute(sql, user);
    }

    // Upsert / Reactivate if soft-deleted
    public int Upsert(User user)
    {
        const string sqlCheck = @"
            SELECT Id, IsDeleted
            FROM Users
            WHERE Email = @Email
        ";

        const string sqlReactivate = @"
            UPDATE Users
            SET IsDeleted = 0,
                DeletedAt = NULL,
                Name = @Name,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id
        ";

        const string sqlInsert = @"
            INSERT INTO Users (Email, Name)
            VALUES (@Email, @Name);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
        ";

        using var db = Connection;
        var existing = db.QuerySingleOrDefault(sqlCheck, new { user.Email });

        if (existing != null)
        {
            if ((bool)existing.IsDeleted)
            {
                db.Execute(sqlReactivate, new { Id = (int)existing.Id, user.Name });
                return (int)existing.Id;
            }
            else
            {
                throw new InvalidOperationException($"User with email '{user.Email}' already exists.");
            }
        }

        return db.QuerySingle<int>(sqlInsert, user);
    }
}

# POCO Model
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

# Repository Result for Paging
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
}
```

# Feature: Search Advanced with Dynamic Sorting
Enhance the Search method so dynamic sorting is safe from SQL injection. The idea:

+ Only allow sorting on predefined column names.
+ Reject or default if the user input is invalid.

## Updated Search Method with Safe Dynamic Sorting
```
public PagedResult<User> Search(
    string? searchTerm = null,
    string? sortColumn = null,
    bool sortDescending = false,
    int page = 1,
    int pageSize = 20,
    bool includeDeleted = false)
{
    using var db = Connection;

    // Allowed columns for sorting
    var allowedSortColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "Email", "Name", "CreatedAt", "UpdatedAt", "DeletedAt", "IsDeleted"
    };

    // Validate sort column
    string orderBy = "Id"; // default
    if (!string.IsNullOrWhiteSpace(sortColumn) && allowedSortColumns.Contains(sortColumn))
    {
        orderBy = sortColumn;
    }

    string sortDirection = sortDescending ? "DESC" : "ASC";

    // Query with COUNT and paging
    string sql = $@"
        SELECT COUNT(*) FROM Users
        WHERE (@IncludeDeleted = 1 OR IsDeleted = 0)
          AND (@SearchTerm IS NULL OR Email LIKE '%' + @SearchTerm + '%' OR Name LIKE '%' + @SearchTerm + '%');

        SELECT * FROM Users
        WHERE (@IncludeDeleted = 1 OR IsDeleted = 0)
          AND (@SearchTerm IS NULL OR Email LIKE '%' + @SearchTerm + '%' OR Name LIKE '%' + @SearchTerm + '%')
        ORDER BY {orderBy} {sortDirection}
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
    ";

    var multi = db.QueryMultiple(sql, new
    {
        IncludeDeleted = includeDeleted ? 1 : 0,
        SearchTerm = searchTerm,
        Offset = (page - 1) * pageSize,
        PageSize = pageSize
    });

    var totalCount = multi.ReadSingle<int>();
    var items = multi.Read<User>();

    return new PagedResult<User>
    {
        TotalCount = totalCount,
        Items = items
    };
}
```

## Usage Example
```
var result = repo.Search(
    searchTerm: "alice",
    sortColumn: "Email",  // safe
    sortDescending: true,
    page: 1,
    pageSize: 10
);

foreach (var u in result.Items)
{
    Console.WriteLine($"{u.Id}: {u.Email} - {u.Name}");
}
```

# Connection String Example
```
Server=localhost;Database=YourDatabaseName;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;
# This ensures:
#   + Encrypted connection (Encrypt=True)
#   + Trust self-signed certificates (TrustServerCertificate=True)
#   + Integrated Windows authentication (Trusted_Connection=True)
```

# Project File Content
```
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <None Remove="appsettings.json" />
    <None Remove="Scripts\001-CreateUserTable.sql" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
    <Content Include="Scripts\001-CreateUserTable.sql">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Dapper" Version="2.1.66" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="6.1.1" />
    <PackageReference Include="Serilog" Version="4.3.0" />
    <PackageReference Include="Serilog.Extensions.Hosting" Version="9.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
  </ItemGroup>

</Project>
```

# Serilog Configuration in Code
```
using System.Text;

using Serilog;

class Program
{
    static void Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()                 // Minimum log level
            .WriteTo.Console()                    // Output logs to console
            .WriteTo.File("logs/log-.txt",        // Output logs to rolling file
                          rollingInterval: RollingInterval.Day,
                          retainedFileCountLimit: 7,
                          encoding: Encoding.UTF8,
                          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Application Starting...");

            // Example logs
            Log.Debug("This is a debug message.");
            Log.Warning("This is a warning message.");
            Log.Error("This is an error message.");

            Console.WriteLine("Hello, Serilog!");
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
```

## Details on Serilog Configuration
```
# dotnet add package Microsoft.Extensions.Configuration --version 8.0.0
# dotnet add package Microsoft.Extensions.Configuration.FileExtensions --version 8.0.0
# dotnet add package Microsoft.Extensions.Configuration.Json --version 8.0.0

using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory) // <-- requires Microsoft.Extensions.Configuration.FileExtensions
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

# dotnet add package Serilog
# dotnet add package Serilog.Sinks.Console
# dotnet add package Serilog.Sinks.File
# dotnet add package Serilog.Extensions.Hosting --version 8.0.0
# dotnet add package Serilog.Settings.Configuration --version 8.0.0

using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration) // <-- requires Serilog.Settings.Configuration
    .CreateLogger();
```

# Exception Handling

Ensure that all repository methods handle exceptions gracefully, especially for database operations. Use Serilog for logging errors.

## 001-CreateUserTable.sql
```
SQL80001: Incorrect syntax: 'CREATE TRIGGER' must be the only statement in the batch.
```

Because CREATE TRIGGER must be executed alone.

Solution: Use GO Separators and Execute by Batch

Important Note:
+ GO is not a T-SQL command; it's a batch separator recognized by SQL Server Management Studio (SSMS).
+ When using ADO.NET or Dapper, you need to split manually.

## Could not find a part of the path 'Scripts\001-CreateUserTable.sql'
```
System.IO.DirectoryNotFoundException: Could not find a part of the path 'D:\gtechsltn\DapperRepository\src\DapperRepository\bin\Debug\net8.0\Scripts'.
   at System.IO.Enumeration.FileSystemEnumerator`1.CreateDirectoryHandle(String path, Boolean ignoreNotFound)
   at System.IO.Enumeration.FileSystemEnumerator`1.Init()
   at System.IO.Enumeration.FileSystemEnumerable`1..ctor(String directory, FindTransform transform, EnumerationOptions options, Boolean isNormalized)
   at System.IO.Enumeration.FileSystemEnumerableFactory.UserFiles(String directory, String expression, EnumerationOptions options)
   at System.IO.Directory.InternalEnumeratePaths(String path, String searchPattern, SearchTarget searchTarget, EnumerationOptions options)
   at System.IO.Directory.GetFiles(String path, String searchPattern, EnumerationOptions enumerationOptions)
   at DapperRepository.Program.Main(String[] args) in D:\gtechsltn\DapperRepository\src\DapperRepository\Program.cs:line 68
```

## Example of Executing Multiple Scripts with GO
```
2025-08-20 10:49:10 [ERR] Transaction rolled back due to error.
Microsoft.Data.SqlClient.SqlException (0x80131904): Incorrect syntax near 'GO'.
   at Microsoft.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
   at Microsoft.Data.SqlClient.SqlInternalConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
   at Microsoft.Data.SqlClient.TdsParser.ThrowExceptionAndWarning(TdsParserStateObject stateObj, SqlCommand command, Boolean callerHasConnectionLock, Boolean asyncClose)
   at Microsoft.Data.SqlClient.TdsParser.TryRun(RunBehavior runBehavior, SqlCommand cmdHandler, SqlDataReader dataStream, BulkCopySimpleResultSet bulkCopyHandler, TdsParserStateObject stateObj, Boolean& dataReady)
   at Microsoft.Data.SqlClient.SqlCommand.RunExecuteNonQueryTds(String methodName, Boolean isAsync, Int32 timeout, Boolean asyncWrite)
   at Microsoft.Data.SqlClient.SqlCommand.InternalExecuteNonQuery(TaskCompletionSource`1 completion, Boolean sendToPipe, Int32 timeout, Boolean& usedCache, Boolean asyncWrite, Boolean inRetry, String methodName)
   at Microsoft.Data.SqlClient.SqlCommand.ExecuteNonQuery()
   at Dapper.SqlMapper.ExecuteCommand(IDbConnection cnn, CommandDefinition& command, Action`2 paramReader) in /_/Dapper/SqlMapper.cs:line 2994
   at Dapper.SqlMapper.ExecuteImpl(IDbConnection cnn, CommandDefinition& command) in /_/Dapper/SqlMapper.cs:line 685
   at Dapper.SqlMapper.Execute(IDbConnection cnn, String sql, Object param, IDbTransaction transaction, Nullable`1 commandTimeout, Nullable`1 commandType) in /_/Dapper/SqlMapper.cs:line 556
   at DapperRepository.Program.Main(String[] args) in D:\gtechsltn\DapperRepository\src\DapperRepository\Program.cs:line 89
ClientConnectionId:37da7229-c3db-48e1-a54d-abf79e91f255
Error Number:102,State:1,Class:15
```

```
2025-08-20 10:59:19 [FTL] Application terminated unexpectedly!
Microsoft.Data.SqlClient.SqlException (0x80131904): Incorrect syntax near 'GO'.
'CREATE TRIGGER' must be the first statement in a query batch.
   at Microsoft.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
   at Microsoft.Data.SqlClient.SqlInternalConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)
   at Microsoft.Data.SqlClient.TdsParser.ThrowExceptionAndWarning(TdsParserStateObject stateObj, SqlCommand command, Boolean callerHasConnectionLock, Boolean asyncClose)
   at Microsoft.Data.SqlClient.TdsParser.TryRun(RunBehavior runBehavior, SqlCommand cmdHandler, SqlDataReader dataStream, BulkCopySimpleResultSet bulkCopyHandler, TdsParserStateObject stateObj, Boolean& dataReady)
   at Microsoft.Data.SqlClient.SqlCommand.RunExecuteNonQueryTds(String methodName, Boolean isAsync, Int32 timeout, Boolean asyncWrite)
   at Microsoft.Data.SqlClient.SqlCommand.InternalExecuteNonQuery(TaskCompletionSource`1 completion, Boolean sendToPipe, Int32 timeout, Boolean& usedCache, Boolean asyncWrite, Boolean inRetry, String methodName)
   at Microsoft.Data.SqlClient.SqlCommand.ExecuteNonQuery()
   at Dapper.SqlMapper.ExecuteCommand(IDbConnection cnn, CommandDefinition& command, Action`2 paramReader) in /_/Dapper/SqlMapper.cs:line 2994
   at Dapper.SqlMapper.ExecuteImpl(IDbConnection cnn, CommandDefinition& command) in /_/Dapper/SqlMapper.cs:line 685
   at Dapper.SqlMapper.Execute(IDbConnection cnn, String sql, Object param, IDbTransaction transaction, Nullable`1 commandTimeout, Nullable`1 commandType) in /_/Dapper/SqlMapper.cs:line 556
   at DapperRepository.Program.Main(String[] args) in D:\gtechsltn\DapperRepository\src\DapperRepository\Program.cs:line 89
ClientConnectionId:4fd348ff-c806-4084-9584-8eef12d71193
Error Number:102,State:1,Class:15
```

Solution: Split the script by GO and execute each part separately.
```
CREATE TABLE Users (
	Id INT IDENTITY(1, 1) PRIMARY KEY
	,Email NVARCHAR(255) NOT NULL
	,Name NVARCHAR(255) NOT NULL
	,IsDeleted BIT NOT NULL DEFAULT 0
	,DeletedAt DATETIME NULL
	,CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE()
	,UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE()
	);

CREATE UNIQUE INDEX UQ_Users_Email_NotDeleted ON Users (Email)
WHERE IsDeleted = 0;

GO

-- IMPORTANT: Use GO to separate batches

CREATE TRIGGER TRG_Users_UpdateTimestamp ON Users
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE Users
	SET UpdatedAt = GETUTCDATE()
	FROM Users u
	INNER JOIN inserted i
		ON u.Id = i.Id;
END;
```

## => Key Rules for GO
+ Must be on a line by itself.
+ Cannot have comments after it (GO --comment will break batch splitting in C#).
+ Leading/trailing whitespace is fine ( GO ).
+ When executing scripts in C#, we usually split by \nGO\n or \r\nGO\r\n.
```
var batches = sql.Split(new[] { "\r\nGO\r\n", "\nGO\n", "\rGO\r" }, StringSplitOptions.RemoveEmptyEntries);
foreach (var batch in batches)
{
    if (!string.IsNullOrWhiteSpace(batch))
    {
        db.Execute(batch);
    }
}
```

## => If Using C# Script Runner

Make sure your runner splits scripts on GO and executes each batch individually.

This way, dropping and creating triggers, procedures, or functions works correctly.

# Conclusion

## => Rule of Thumb:

| Object Type | Batch Requirement |
| --- | --- |
| Table | Can be inside IF NOT EXISTS ... BEGIN ... END |
| Index | Can be inside IF NOT EXISTS ... |
| Trigger | Must be in its own batch, cannot wrap in IF |
| Procedure | Same as Trigger |
| Function | Same as Trigger |

**Rewrite your Users table + trigger script so it's idempotent, works with Dapper, GO-separated, and won't throw batch/exists errors.**

```
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
```

# Unit Tests with Moq + xUnit + FluentAssertions
```
dotnet add package FluentAssertions --version 6.12.1
dotnet add package Moq --version 4.20.72
dotnet add package xunit --version 2.8.2
dotnet add package xunit.runner.visualstudio --version 2.8.2
dotnet add package coverlet.collector --version 6.0.0
dotnet add package Microsoft.NET.Test.Sdk --version 17.5.0
```

# Integration Tests
```
dotnet add package FluentAssertions --version 6.12.1
dotnet add package xunit --version 2.8.2
dotnet add package xunit.runner.visualstudio --version 2.8.2
dotnet add package coverlet.collector --version 6.0.0
dotnet add package Microsoft.NET.Test.Sdk --version 17.5.0
```

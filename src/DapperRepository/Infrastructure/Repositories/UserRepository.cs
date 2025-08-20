using Dapper;

using DapperRepository.Application.Dtos;
using DapperRepository.Application.Helpers;
using DapperRepository.Application.Interfaces;
using DapperRepository.Domain.Entities;

namespace DapperRepository.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDapperWrapper _dapper;

    public UserRepository(IDbConnectionFactory connectionFactory, IDapperWrapper dapper)
    {
        _connectionFactory = connectionFactory;
        _dapper = dapper;
    }

    // Get all users (optional filtering by IsDeleted)
    public IEnumerable<User> GetAll(bool includeDeleted = false)
    {
        string sql = "SELECT * FROM Users WHERE (@IncludeDeleted = 1 OR IsDeleted = 0)";
        using var db = _connectionFactory.CreateConnection();
        return _dapper.Query<User>(db, sql, new { IncludeDeleted = includeDeleted ? 1 : 0 });
    }

    // Get by Id (optional filtering by IsDeleted)
    public User? GetById(int id, bool includeDeleted = false)
    {
        string sql = "SELECT * FROM Users WHERE Id = @Id AND (@IncludeDeleted = 1 OR IsDeleted = 0)";
        using var db = _connectionFactory.CreateConnection();
        return _dapper.QuerySingleOrDefault<User>(db, sql, new { Id = id, IncludeDeleted = includeDeleted ? 1 : 0 });
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
        // Sanitize paging parameters
        (page, pageSize) = PagingHelper.Sanitize(page, pageSize);

        using var db = _connectionFactory.CreateConnection();

        // Normalize SearchTerm: null if empty or whitespace
        string? normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        var sql = @"
            -- Count
            SELECT COUNT(*) 
            FROM Users
            WHERE (@IncludeDeleted = 1 OR IsDeleted = 0)
              AND (@SearchTerm IS NULL OR Email LIKE '%' + @SearchTerm + '%' OR Name LIKE '%' + @SearchTerm + '%');

            -- Paged result
            SELECT * 
            FROM Users
            WHERE (@IncludeDeleted = 1 OR IsDeleted = 0)
              AND (@SearchTerm IS NULL OR Email LIKE '%' + @SearchTerm + '%' OR Name LIKE '%' + @SearchTerm + '%')
        ";

        // Add sorting
        if (!string.IsNullOrWhiteSpace(sortColumn))
        {
            sql += $" ORDER BY {sortColumn} {(sortDescending ? "DESC" : "ASC")}";
        }
        else
        {
            sql += " ORDER BY Id"; // default sort
        }

        // Add pagination
        sql += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        using var multi = _dapper.QueryMultiple(db, sql, new
        {
            SearchTerm = normalizedSearch,
            IncludeDeleted = includeDeleted ? 1 : 0,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });

        var totalCount = multi.ReadSingleAsync<int>().Result;
        var items = multi.ReadAsync<User>().Result.ToArray();

        return new PagedResult<User>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
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

        using var db = _connectionFactory.CreateConnection();
        _dapper.Execute(db, sql, new { Id = id });
    }

    // Insert user
    public int Insert(User user)
    {
        const string sql = @"
            INSERT INTO Users (Email, Name)
            VALUES (@Email, @Name);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
        ";

        using var db = _connectionFactory.CreateConnection();
        return _dapper.QuerySingle<int>(db, sql, user);
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

        using var db = _connectionFactory.CreateConnection();
        _dapper.Execute(db, sql, user);
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

        using var db = _connectionFactory.CreateConnection();
        var existing = _dapper.QuerySingleOrDefault<User>(db, sqlCheck, new { user.Email });

        if (existing != null)
        {
            if (existing.IsDeleted)
            {
                db.Execute(sqlReactivate, new { Id = existing.Id, user.Name });
                return existing.Id;
            }
            else
            {
                throw new InvalidOperationException($"User with email '{user.Email}' already exists.");
            }
        }

        return _dapper.QuerySingle<int>(db, sqlInsert, user);
    }
}
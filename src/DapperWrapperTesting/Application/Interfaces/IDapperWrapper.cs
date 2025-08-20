using System.Data;

namespace DapperWrapperTesting.Application.Interfaces;

public interface IDapperWrapper
{
    // Basic Query
    Task<IEnumerable<T>> QueryAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null);

    Task<T> QueryFirstAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null);

    Task<T?> QueryFirstOrDefaultAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null);

    Task<T> QuerySingleAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null);

    Task<T?> QuerySingleOrDefaultAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null);

    // Commands
    Task<int> ExecuteAsync(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null);

    Task<T?> ExecuteScalarAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null);

    // Multi-result sets
    Task<(IEnumerable<T1>, IEnumerable<T2>)> QueryMultipleAsync<T1, T2>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null);

    // Multi-mapping
    Task<IEnumerable<TReturn>> QueryAsync<T1, T2, TReturn>(
        IDbConnection connection,
        string sql,
        Func<T1, T2, TReturn> map,
        object? param = null,
        IDbTransaction? transaction = null,
        string splitOn = "Id");

    Task<IEnumerable<TReturn>> QueryAsync<T1, T2, T3, TReturn>(
        IDbConnection connection,
        string sql,
        Func<T1, T2, T3, TReturn> map,
        object? param = null,
        IDbTransaction? transaction = null,
        string splitOn = "Id");
}

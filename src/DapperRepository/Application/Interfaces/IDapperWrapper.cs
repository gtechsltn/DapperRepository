using System.Data;

namespace DapperRepository.Application.Interfaces;

public interface IDapperWrapper
{
    // Query - returns multiple rows
    IEnumerable<T> Query<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, bool buffered = true, int? commandTimeout = null,
        CommandType? commandType = null);

    // QueryAsync
    Task<IEnumerable<T>> QueryAsync<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    // QuerySingle / QuerySingleOrDefault
    T QuerySingle<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    T? QuerySingleOrDefault<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    Task<T> QuerySingleAsync<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    Task<T?> QuerySingleOrDefaultAsync<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    // QueryFirst / QueryFirstOrDefault
    T QueryFirst<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    T? QueryFirstOrDefault<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    Task<T> QueryFirstAsync<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    Task<T?> QueryFirstOrDefaultAsync<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    // Execute
    int Execute(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    Task<int> ExecuteAsync(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    // ExecuteScalar
    T? ExecuteScalar<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    Task<T?> ExecuteScalarAsync<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    // QueryMultiple
    IMultiResultReader QueryMultiple(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);

    Task<IMultiResultReader> QueryMultipleAsync(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null);
}

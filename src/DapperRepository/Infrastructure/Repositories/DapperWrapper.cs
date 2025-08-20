using System.Data;

using Dapper;

using DapperRepository.Application.Interfaces;

namespace DapperRepository.Infrastructure.Repositories;

public class DapperWrapper : IDapperWrapper
{
    public IEnumerable<T> Query<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, bool buffered = true, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.Query<T>(sql, param, transaction, buffered, commandTimeout, commandType);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return await connection.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public T QuerySingle<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.QuerySingle<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public T? QuerySingleOrDefault<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.QuerySingleOrDefault<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<T> QuerySingleAsync<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return await connection.QuerySingleAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return await connection.QuerySingleOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public T QueryFirst<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.QueryFirst<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public T? QueryFirstOrDefault<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.QueryFirstOrDefault<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<T> QueryFirstAsync<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return await connection.QueryFirstAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return await connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public int Execute(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.Execute(sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<int> ExecuteAsync(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return await connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
    }

    public T? ExecuteScalar<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.ExecuteScalar<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<T?> ExecuteScalarAsync<T>(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return await connection.ExecuteScalarAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public IMultiResultReader QueryMultiple(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        var gridReader = connection.QueryMultiple(sql, param, transaction, commandTimeout, commandType);
        return new DapperGridReaderWrapper(gridReader);
    }

    public async Task<IMultiResultReader> QueryMultipleAsync(IDbConnection connection, string sql, object? param = null,
        IDbTransaction? transaction = null, int? commandTimeout = null,
        CommandType? commandType = null)
    {
        var gridReader = await connection.QueryMultipleAsync(sql, param, transaction, commandTimeout, commandType);
        return new DapperGridReaderWrapper(gridReader);
    }
}
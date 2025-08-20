namespace DapperWrapperTesting.Infrastructure;

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using Dapper;

using DapperWrapperTesting.Application.Interfaces;

public class DapperWrapper : IDapperWrapper
{
    public async Task<IEnumerable<T>> QueryAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null)
        => await connection.QueryAsync<T>(sql, param, transaction);

    public async Task<T> QueryFirstAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null)
        => await connection.QueryFirstAsync<T>(sql, param, transaction);

    public async Task<T?> QueryFirstOrDefaultAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null)
        => await connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction);

    public async Task<T> QuerySingleAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null)
        => await connection.QuerySingleAsync<T>(sql, param, transaction);

    public async Task<T?> QuerySingleOrDefaultAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null)
        => await connection.QuerySingleOrDefaultAsync<T>(sql, param, transaction);

    public async Task<int> ExecuteAsync(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null)
        => await connection.ExecuteAsync(sql, param, transaction);

    public async Task<T?> ExecuteScalarAsync<T>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null)
        => await connection.ExecuteScalarAsync<T>(sql, param, transaction);

    public async Task<(IEnumerable<T1>, IEnumerable<T2>)> QueryMultipleAsync<T1, T2>(IDbConnection connection, string sql, object? param = null, IDbTransaction? transaction = null)
    {
        using var multi = await connection.QueryMultipleAsync(sql, param, transaction);
        var result1 = await multi.ReadAsync<T1>();
        var result2 = await multi.ReadAsync<T2>();
        return (result1, result2);
    }

    public Task<IEnumerable<TReturn>> QueryAsync<T1, T2, TReturn>(
        IDbConnection connection,
        string sql,
        Func<T1, T2, TReturn> map,
        object? param = null,
        IDbTransaction? transaction = null,
        string splitOn = "Id")
        => connection.QueryAsync(sql, map, param, transaction, splitOn: splitOn);

    public Task<IEnumerable<TReturn>> QueryAsync<T1, T2, T3, TReturn>(
        IDbConnection connection,
        string sql,
        Func<T1, T2, T3, TReturn> map,
        object? param = null,
        IDbTransaction? transaction = null,
        string splitOn = "Id")
        => connection.QueryAsync(sql, map, param, transaction, splitOn: splitOn);
}

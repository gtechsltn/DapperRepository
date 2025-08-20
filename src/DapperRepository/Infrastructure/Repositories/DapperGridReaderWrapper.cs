using Dapper;

using DapperRepository.Application.Interfaces;

namespace DapperRepository.Infrastructure.Repositories;

public class DapperGridReaderWrapper : IMultiResultReader
{
    private readonly SqlMapper.GridReader _gridReader;

    public DapperGridReaderWrapper(SqlMapper.GridReader gridReader)
    {
        _gridReader = gridReader;
    }

    public async Task<IEnumerable<T>> ReadAsync<T>()
    {
        return await _gridReader.ReadAsync<T>();
    }

    public async Task<T> ReadSingleAsync<T>()
    {
        return await _gridReader.ReadSingleAsync<T>();
    }

    public async Task<T> ReadFirstAsync<T>()
    {
        return await _gridReader.ReadFirstAsync<T>();
    }

    public void Dispose()
    {
        _gridReader.Dispose();
    }
}
using Dapper;

namespace DapperRepository.Application.Interfaces;

public class DapperGridReader : IGridReader
{
    private readonly SqlMapper.GridReader _reader;

    public DapperGridReader(SqlMapper.GridReader reader)
    {
        _reader = reader;
    }

    public async Task<IEnumerable<T>> ReadAsync<T>()
        => await _reader.ReadAsync<T>();

    public void Dispose() => _reader.Dispose();
}
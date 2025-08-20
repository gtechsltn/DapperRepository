namespace DapperRepository.Application.Interfaces;

public interface IMultiResultReader : IDisposable
{
    Task<IEnumerable<T>> ReadAsync<T>();
    Task<T> ReadSingleAsync<T>();
    Task<T> ReadFirstAsync<T>();
}
namespace DapperRepository.Application.Interfaces;

public interface IGridReader : IDisposable
{
    Task<IEnumerable<T>> ReadAsync<T>();
}

using DapperRepository.Application.Dtos;
using DapperRepository.Domain.Entities;

namespace DapperRepository.Application.Interfaces
{
    public interface IUserRepository
    {
        IEnumerable<User> GetAll(bool includeDeleted = false);
        User? GetById(int id, bool includeDeleted = false);
        int Insert(User user);
        PagedResult<User> Search(string? searchTerm = null, string? sortColumn = null, bool sortDescending = false, int page = 1, int pageSize = 20, bool includeDeleted = false);
        void SoftDelete(int id);
        void Update(User user);
        int Upsert(User user);
    }
}
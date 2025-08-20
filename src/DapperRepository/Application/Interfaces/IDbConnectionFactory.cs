using System.Data;

namespace DapperRepository.Application.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

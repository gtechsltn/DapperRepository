using System.Data;

using DapperRepository.Application.Interfaces;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace DapperRepository.Infrastructure.Repositories;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        // Expect connection string in appsettings.json
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
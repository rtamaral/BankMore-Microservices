using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BankMore.Infrastructure.Database
{
    public class SqlServerConnectionFactory
    {
        private readonly string _connectionString;

        public SqlServerConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public SqlServerConnectionFactory(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);
    }
}
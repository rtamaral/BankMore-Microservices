using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BankMore.Infrastructure.Database
{
    public class SqlServerConnectionFactory
    {
        private readonly string _connectionString;

        // Construtor que aceita IConfiguration (seu código atual)
        public SqlServerConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        // Construtor alternativo que aceita string diretamente
        public SqlServerConnectionFactory(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);
    }
}
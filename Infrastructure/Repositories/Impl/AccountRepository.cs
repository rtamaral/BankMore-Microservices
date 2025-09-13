using BankMore.Domain.Entities;
using BankMore.Infrastructure.Database;
using Dapper;
using System.Data;

namespace BankMore.Infrastructure.Repositories.Impl
{
    public class AccountRepository : IAccountRepository
    {
        private readonly SqlServerConnectionFactory _connectionFactory;

        public AccountRepository(SqlServerConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CreateAsync(Account account)
        {
            using IDbConnection db = _connectionFactory.CreateConnection();
            string sql = @"INSERT INTO contacorrente (idcontacorrente, numero, nome, ativo, senha, salt)
                           VALUES (@Id, @Numero, @Nome, @Ativo, @Senha, @Salt)";
            return await db.ExecuteAsync(sql, account);
        }

        public async Task<Account?> GetByCpfAsync(string cpf)
        {
            using IDbConnection db = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM contacorrente WHERE nome = @Cpf"; // ajustar se CPF for coluna separada
            return await db.QueryFirstOrDefaultAsync<Account>(sql, new { Cpf = cpf });
        }

        public async Task<Account?> GetByIdAsync(Guid accountId)
        {
            using IDbConnection db = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM contacorrente WHERE idcontacorrente = @Id";
            return await db.QueryFirstOrDefaultAsync<Account>(sql, new { Id = accountId });
        }

        public async Task<Account?> GetByNumberAsync(int accountNumber)
        {
            using IDbConnection db = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM contacorrente WHERE numero = @Numero";
            return await db.QueryFirstOrDefaultAsync<Account>(sql, new { Numero = accountNumber });
        }

        public async Task<bool> UpdateStatusAsync(Guid accountId, bool active)
        {
            using IDbConnection db = _connectionFactory.CreateConnection();
            string sql = "UPDATE contacorrente SET ativo = @Ativo WHERE idcontacorrente = @Id";
            int rows = await db.ExecuteAsync(sql, new { Ativo = active ? 1 : 0, Id = accountId });
            return rows > 0;
        }
    }
}

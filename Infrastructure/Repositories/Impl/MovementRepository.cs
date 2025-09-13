using BankMore.Domain.Entities;
using BankMore.Infrastructure.Database;
using Dapper;
using System.Data;

namespace BankMore.Infrastructure.Repositories.Impl
{
    public class MovementRepository : IMovementRepository
    {
        private readonly SqlServerConnectionFactory _connectionFactory;

        public MovementRepository(SqlServerConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CreateMovementAsync(Movement movement)
        {
            using IDbConnection db = _connectionFactory.CreateConnection();

            string sql = @"INSERT INTO movimento (idmovimento, idcontacorrente, tipomovimento, valor, datamovimento)
                           VALUES (@Id, @AccountId, @Type, @Value, @CreatedAt)";

            return await db.ExecuteAsync(sql, movement);
        }

        public async Task<decimal> GetBalanceAsync(Guid accountId)
        {
            using IDbConnection db = _connectionFactory.CreateConnection();

            string sql = @"
                SELECT ISNULL(SUM(CASE WHEN tipomovimento='C' THEN valor ELSE 0 END),0) -
                       ISNULL(SUM(CASE WHEN tipomovimento='D' THEN valor ELSE 0 END),0)
                FROM movimento
                WHERE idcontacorrente = @AccountId";

            return await db.ExecuteScalarAsync<decimal>(sql, new { AccountId = accountId });
        }
    }
}

using BankMore.Domain.Entities;
using BankMore.Infrastructure.Database;
using Dapper;
using System.Data;

namespace BankMore.Infrastructure.Repositories.Impl
{
    public class TransferRepository : ITransferRepository
    {
        private readonly SqlServerConnectionFactory _connectionFactory;

        public TransferRepository(SqlServerConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> CreateTransferAsync(Transfer transfer)
        {
            using IDbConnection db = _connectionFactory.CreateConnection();
            string sql = @"INSERT INTO transferencia (idtransferencia, idcontacorrente_origem, idcontacorrente_destino, valor, datamovimento)
                           VALUES (@Id, @ContaOrigemId, @ContaDestinoId, @Valor, @Data)";
            return await db.ExecuteAsync(sql, transfer);
        }
    }
}

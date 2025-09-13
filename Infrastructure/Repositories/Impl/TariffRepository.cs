using BankMore.Api.Domain.Entities;
using Dapper;
using System.Data;
using System.Threading.Tasks;

namespace BankMore.Infrastructure.Repositories.Impl
{
    public class TariffRepository : ITariffRepository
    {
        private readonly IDbConnection _dbConnection;

        public TariffRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        }

        public async Task CreateTariffAsync(Tariff tariff)
        {
            const string sql = @"
                INSERT INTO tarifa (idtarifa, idcontacorrente, datamovimento, valor)
                VALUES (@IdTarifa, @IdContaCorrente, @DataMovimento, @Valor)";

            await _dbConnection.ExecuteAsync(sql, new
            {
                IdTarifa = tariff.IdTarifa,
                IdContaCorrente = tariff.IdContaCorrente,
                DataMovimento = tariff.DataMovimento,
                Valor = tariff.Valor
            });
        }
    }
}

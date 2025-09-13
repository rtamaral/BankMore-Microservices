using BankMore.Api.Application.Shared.DTOs;
using BankMore.Infrastructure.Database;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankMore.Application.Queries.Handlers
{
    public class GetBalanceQueryHandler : IRequestHandler<GetBalanceQuery, AccountBalanceDto>
    {
        private readonly SqlServerConnectionFactory _connectionFactory;
        private readonly ILogger<GetBalanceQueryHandler> _logger;

        public GetBalanceQueryHandler(SqlServerConnectionFactory connectionFactory, ILogger<GetBalanceQueryHandler> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<AccountBalanceDto> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
        {
            try
            {
                using IDbConnection db = _connectionFactory.CreateConnection();

                // Consulta saldo
                string sqlBalance = @"
                    SELECT 
                    COALESCE(SUM(CASE WHEN tipomovimento = 'C' THEN valor ELSE 0 END), 0) -
                    COALESCE(SUM(CASE WHEN tipomovimento = 'D' THEN valor ELSE 0 END), 0) AS Balance
                    FROM movimento
                    WHERE idcontacorrente = @AccountId";


                decimal balance = await db.ExecuteScalarAsync<decimal>(sqlBalance, new { request.AccountId });

                // Consulta dados da conta
                string sqlAccount = @"
                SELECT numero AS AccountNumber, nome AS Name, cpf AS Cpf
                FROM contacorrente
                WHERE idcontacorrente = @AccountId";



                var account = await db.QueryFirstOrDefaultAsync<dynamic>(sqlAccount, new { request.AccountId });

                if (account == null)
                {
                    _logger.LogWarning("Conta não encontrada: {AccountId}", request.AccountId);
                    return null;
                }

                return new AccountBalanceDto(
                    account.AccountNumber,
                    account.Name,
                    account.Cpf,
                    balance,
                    DateTime.UtcNow
                );


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar saldo da conta {AccountId}", request.AccountId);
                throw;
            }
        }
    }
}

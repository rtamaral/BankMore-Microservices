using BankMore.Api.Application.Shared.DTOs;
using BankMore.Application.Queries;
using BankMore.Infrastructure.Database;
using Dapper;
using MediatR;
using System.Data;

namespace BankMore.Application.Handlers
{
    public class GetBalanceQueryHandler
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
            COALESCE(SUM(CASE WHEN tipo = 'CREDITO' THEN valor ELSE 0 END),0) -
            COALESCE(SUM(CASE WHEN tipo = 'DEBITO' THEN valor ELSE 0 END),0) AS Balance
        FROM movimento
        WHERE idcontacorrente = @AccountId";

                decimal balance = await db.ExecuteScalarAsync<decimal>(sqlBalance, new { request.AccountId });

                // Consulta dados da conta
                string sqlAccount = @"
        SELECT numero, nome
        FROM contacorrente
        WHERE idcontacorrente = @AccountId";

                var account = await db.QueryFirstOrDefaultAsync<dynamic>(sqlAccount, new { request.AccountId });

                if (account == null)
                {
                    _logger.LogWarning("Conta não encontrada: {AccountId}", request.AccountId);
                    return null; // ou lance uma exceção específica
                }

                // Passando a data da consulta
                return new AccountBalanceDto(
                    (int)account.numero,
                    (string)account.nome,
                    (string)account.cpf,   // novo campo
                    balance,
                    DateTime.UtcNow         // data da consulta
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

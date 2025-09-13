using BankMore.Api.Application.Queries;
using BankMore.Api.Application.Shared.DTOs;
using BankMore.Infrastructure.Database;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankMore.Api.Application.Commands.Handlers
{
    public class GetMovementsHandler : IRequestHandler<GetMovementsQuery, MovementHistoryDto>
    {
        private readonly SqlServerConnectionFactory _connectionFactory;
        private readonly ILogger<GetMovementsHandler> _logger;

        public GetMovementsHandler(SqlServerConnectionFactory connectionFactory, ILogger<GetMovementsHandler> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<MovementHistoryDto> Handle(GetMovementsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                using IDbConnection db = _connectionFactory.CreateConnection();

                // Consulta dados da conta
                var account = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT numero AS AccountNumber, nome AS Name FROM contacorrente WHERE idcontacorrente = @AccountId",
                    new { request.AccountId }
                );

                if (account == null)
                {
                    _logger.LogWarning("Conta não encontrada: {AccountId}", request.AccountId);
                    throw new ArgumentException("Conta não encontrada");
                }

                // Consulta movimentações
                var movements = (await db.QueryAsync<MovementDto>(
                    "SELECT idmovimento AS MovementId, idcontacorrente AS AccountId, tipomovimento AS Type, valor AS Value, datamovimento AS Date " +
                    "FROM movimento WHERE idcontacorrente = @AccountId ORDER BY datamovimento DESC",
                    new { request.AccountId }
                )).ToList();

                return new MovementHistoryDto((int)account.AccountNumber, (string)account.Name, movements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar movimentações da conta {AccountId}", request.AccountId);
                throw;
            }
        }
    }
}

using BankMore.Api.Domain.Entities;
using BankMore.Domain.Entities;
using BankMore.Infrastructure.Repositories;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace BankMore.Application.Commands.Handlers
{
    public class TransferCommandHandler : IRequestHandler<TransferCommand, bool>
    {
        private readonly IDbConnection _dbConnection;
        private readonly IMovementRepository _movementRepository;
        private readonly ITariffRepository _tariffRepository;
        private readonly ILogger<TransferCommandHandler> _logger;

        private const decimal TariffValue = 2m; // Tarifa fixa de 2 reais

        public TransferCommandHandler(
            IDbConnection dbConnection,
            IMovementRepository movementRepository,
            ITariffRepository tariffRepository,
            ILogger<TransferCommandHandler> logger)
        {
            _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
            _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
            _tariffRepository = tariffRepository ?? throw new ArgumentNullException(nameof(tariffRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Handle(TransferCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Gera IdempotencyKey se não vier
                if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                    request.IdempotencyKey = Guid.NewGuid().ToString();

                if (!Guid.TryParse(request.IdempotencyKey, out var idempotencyGuid))
                    throw new ArgumentException("IdempotencyKey inválida ou ausente.");

                // Verifica idempotência
                var existing = await _dbConnection.QueryFirstOrDefaultAsync<Guid?>(
                    "SELECT chave_idempotencia FROM idempotencia WHERE chave_idempotencia = @IdempotencyKey",
                    new { IdempotencyKey = idempotencyGuid });

                if (existing.HasValue)
                {
                    _logger.LogWarning(
                        "Transferência duplicada detectada. IdempotencyKey={IdempotencyKey}, SourceAccountId={SourceAccountId}, DestinationAccountId={DestinationAccountId}",
                        idempotencyGuid, request.SourceAccountId, request.DestinationAccountId);

                    return false; // Não processa novamente
                }

                // Consulta saldo da conta de origem
                var saldo = await _dbConnection.QueryFirstOrDefaultAsync<decimal>(
                    @"SELECT 
                        ISNULL(SUM(CASE WHEN tipomovimento = 'C' THEN valor ELSE 0 END),0)
                        - ISNULL(SUM(CASE WHEN tipomovimento = 'D' THEN valor ELSE 0 END),0) AS Saldo
                      FROM movimento
                      WHERE idcontacorrente = @AccountId",
                    new { AccountId = request.SourceAccountId });

                var totalDebit = request.Value + TariffValue;
                if (saldo < totalDebit)
                    throw new InvalidOperationException("Saldo insuficiente para transferência e tarifa.");

                // Movimentos
                var debitMovement = new Movement
                {
                    Id = Guid.NewGuid(),
                    AccountId = request.SourceAccountId,
                    Type = "D",
                    Value = request.Value,
                    CreatedAt = DateTime.UtcNow
                };
                var creditMovement = new Movement
                {
                    Id = Guid.NewGuid(),
                    AccountId = request.DestinationAccountId,
                    Type = "C",
                    Value = request.Value,
                    CreatedAt = DateTime.UtcNow
                };
                var tariffMovement = new Movement
                {
                    Id = Guid.NewGuid(),
                    AccountId = request.SourceAccountId,
                    Type = "D",
                    Value = TariffValue,
                    CreatedAt = DateTime.UtcNow
                };

                await _movementRepository.CreateMovementAsync(debitMovement);
                await _movementRepository.CreateMovementAsync(creditMovement);
                await _movementRepository.CreateMovementAsync(tariffMovement);

                // Salva transferência
                const string insertTransfer = @"
                    INSERT INTO transferencia
                        (idtransferencia, idcontacorrente_origem, idcontacorrente_destino, datamovimento, valor)
                    VALUES
                        (@Id, @SourceAccountId, @DestinationAccountId, @Date, @Value)";
                await _dbConnection.ExecuteAsync(insertTransfer, new
                {
                    Id = idempotencyGuid,
                    SourceAccountId = request.SourceAccountId,
                    DestinationAccountId = request.DestinationAccountId,
                    Date = DateTime.UtcNow,
                    Value = request.Value
                });

                // Salva tarifa na tabela tarifa
                var tariff = new Tariff
                {
                    IdTarifa = Guid.NewGuid(),
                    IdContaCorrente = request.SourceAccountId,
                    DataMovimento = DateTime.UtcNow,
                    Valor = TariffValue
                };
                await _tariffRepository.CreateTariffAsync(tariff);

                // Salva idempotência
                const string insertIdempotency = @"
                    INSERT INTO idempotencia (chave_idempotencia, requisicao, resultado)
                    VALUES (@IdempotencyKey, @RequestJson, @ResultJson)";
                await _dbConnection.ExecuteAsync(insertIdempotency, new
                {
                    IdempotencyKey = idempotencyGuid,
                    RequestJson = Newtonsoft.Json.JsonConvert.SerializeObject(request),
                    ResultJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { Success = true })
                });

                _logger.LogInformation("Transferência e tarifa registradas com sucesso. Source={Source}, Destination={Destination}, Valor={Value}, Tarifa={Tarifa}",
                    request.SourceAccountId, request.DestinationAccountId, request.Value, TariffValue);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar transferência e registrar tarifa. Source={Source}, Destination={Destination}, Valor={Value}",
                    request.SourceAccountId, request.DestinationAccountId, request.Value);
                throw;
            }
        }
    }
}

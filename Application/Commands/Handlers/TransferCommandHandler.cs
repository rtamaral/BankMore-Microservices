using BankMore.Domain.Entities;
using BankMore.Infrastructure.Repositories;
using BankMore.Api.Infrastructure.Messaging;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;
using Newtonsoft.Json;

namespace BankMore.Application.Commands.Handlers
{
    public class TransferCommandHandler : IRequestHandler<TransferCommand, bool>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMovementRepository _movementRepository;
        private readonly IDbConnection _dbConnection;
        private readonly ITransferKafkaProducer _kafkaProducer;
        private readonly ILogger<TransferCommandHandler> _logger;

        public TransferCommandHandler(
            IAccountRepository accountRepository,
            IMovementRepository movementRepository,
            IDbConnection dbConnection,
            ITransferKafkaProducer kafkaProducer,
            ILogger<TransferCommandHandler> logger)
        {
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
            _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
            _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Handle(TransferCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Value <= 0)
                    throw new ArgumentException("Valor da transferência deve ser positivo.");

                if (!Guid.TryParse(request.IdempotencyKey, out var idempotencyGuid))
                    throw new ArgumentException("IdempotencyKey inválida ou ausente.");

                // Verifica idempotência
                var existing = await _dbConnection.QueryFirstOrDefaultAsync<Guid?>(
                    "SELECT chave_idempotencia FROM idempotencia WHERE chave_idempotencia = @IdempotencyKey",
                    new { IdempotencyKey = idempotencyGuid });

                if (existing.HasValue)
                {
                    _logger.LogInformation("Transferência já processada. IdempotencyKey={IdempotencyKey}", idempotencyGuid);
                    return true;
                }

                // Consulta saldo da conta de origem
                var saldo = await _dbConnection.QueryFirstOrDefaultAsync<decimal>(
                    @"SELECT 
                        ISNULL(SUM(CASE WHEN tipomovimento = 'C' THEN valor ELSE 0 END), 0)
                        - ISNULL(SUM(CASE WHEN tipomovimento = 'D' THEN valor ELSE 0 END), 0) AS Saldo
                      FROM movimentacao
                      WHERE idcontacorrente = @AccountId",
                    new { AccountId = request.SourceAccountId });

                if (saldo < request.Value)
                    throw new ArgumentException("Saldo insuficiente na conta de origem.");

                // Criar movimentos
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

                await _movementRepository.CreateMovementAsync(debitMovement);
                await _movementRepository.CreateMovementAsync(creditMovement);

                // Registrar transferência
                string insertTransfer = @"
                    INSERT INTO transferencia (idtransferencia, idcontacorrente_origem, idcontacorrente_destino, datamovimento, valor)
                    VALUES (@Id, @SourceAccountId, @DestinationAccountId, @Date, @Value)";
                await _dbConnection.ExecuteAsync(insertTransfer, new
                {
                    Id = idempotencyGuid,
                    SourceAccountId = request.SourceAccountId,
                    DestinationAccountId = request.DestinationAccountId,
                    Date = DateTime.UtcNow,
                    Value = request.Value
                });

                // Salvar idempotência
                string insertIdempotency = @"
                    INSERT INTO idempotencia (chave_idempotencia, requisicao, resultado)
                    VALUES (@IdempotencyKey, @RequestJson, @ResultJson)";
                await _dbConnection.ExecuteAsync(insertIdempotency, new
                {
                    IdempotencyKey = idempotencyGuid,
                    RequestJson = JsonConvert.SerializeObject(request),
                    ResultJson = JsonConvert.SerializeObject(new { Success = true })
                });

                // Publicar Kafka
                await _kafkaProducer.PublishTransferAsync(
                    idempotencyGuid,
                    request.SourceAccountId,
                    request.DestinationAccountId,
                    request.Value
                );

                _logger.LogInformation(
                    "Transferência processada com sucesso: IdempotencyKey={IdempotencyKey}, Valor={Value}",
                    idempotencyGuid, request.Value);

                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erro de validação na transferência.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar transferência.");
                throw new InvalidOperationException("Erro ao processar transferência.", ex);
            }
        }
    }
}
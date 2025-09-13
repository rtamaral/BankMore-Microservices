using BankMore.Domain.Entities;
using BankMore.Infrastructure.Repositories;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data;

namespace BankMore.Application.Commands.Handlers
{
    public class MovementCommandHandler : IRequestHandler<MovementCommand, bool>
    {
        private readonly IMovementRepository _movementRepository;
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<MovementCommandHandler> _logger;

        public MovementCommandHandler(
            IMovementRepository movementRepository,
            IDbConnection dbConnection,
            ILogger<MovementCommandHandler> logger)
        {
            _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
            _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Handle(MovementCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validação da IdempotencyKey
                if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                    throw new ArgumentException("IdempotencyKey inválida ou ausente.");

                if (!Guid.TryParse(request.IdempotencyKey, out var idempotencyGuid))
                    throw new ArgumentException("IdempotencyKey inválida ou ausente.");

                // Verifica se já existe movimento com a mesma chave de idempotência
                var existingIdempotency = await _dbConnection.QueryFirstOrDefaultAsync<Guid?>(
                    "SELECT chave_idempotencia FROM idempotencia WHERE chave_idempotencia = @IdempotencyKey",
                    new { IdempotencyKey = idempotencyGuid });

                if (existingIdempotency.HasValue)
                {
                    _logger.LogWarning(
                        "IdempotencyKey já utilizada. IdempotencyKey={IdempotencyKey}, AccountId={AccountId}",
                        idempotencyGuid, request.AccountId);

                    // Retorna false para indicar que o movimento não será criado novamente
                    return false;
                }

                // Validação do valor
                if (request.Value <= 0)
                    throw new ArgumentException("Valor inválido para movimento.");

                if (!request.Type.Equals("C", StringComparison.OrdinalIgnoreCase) &&
                        !request.Type.Equals("D", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Tipo de movimento inválido. Deve ser 'C' ou 'D'.");
                }


                // Cria movimento
                var movement = new Movement
                {
                    Id = Guid.NewGuid(),
                    AccountId = request.AccountId,
                    Type = request.Type,
                    Value = request.Value,
                    CreatedAt = DateTime.UtcNow
                };

                await _movementRepository.CreateMovementAsync(movement);

                // Salva na tabela de idempotência
                const string insertIdempotency = @"
                    INSERT INTO idempotencia (chave_idempotencia, requisicao, resultado)
                    VALUES (@IdempotencyKey, @RequestJson, @ResultJson)";

                await _dbConnection.ExecuteAsync(insertIdempotency, new
                {
                    IdempotencyKey = idempotencyGuid,
                    RequestJson = JsonConvert.SerializeObject(request),
                    ResultJson = JsonConvert.SerializeObject(new { Success = true })
                });

                _logger.LogInformation(
                    "Movimento processado com sucesso. IdempotencyKey={IdempotencyKey}, AccountId={AccountId}",
                    idempotencyGuid, request.AccountId);

                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    ex, "Erro de validação no movimento. AccountId={AccountId}", request.AccountId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "Erro inesperado ao processar movimento. AccountId={AccountId}", request.AccountId);
                throw new InvalidOperationException("Erro ao processar movimento.", ex);
            }
        }
    }
}

using KafkaFlow;
using BankMore.Infrastructure.Repositories;
using BankMore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BankMore.Api.Infrastructure.Messaging
{
    public class ContaCorrenteTarifaHandler : IMessageHandler<TransferMessage>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ContaCorrenteTarifaHandler> _logger;

        public ContaCorrenteTarifaHandler(IServiceProvider serviceProvider, ILogger<ContaCorrenteTarifaHandler> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Handle(IMessageContext context, TransferMessage message)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var movementRepository = scope.ServiceProvider.GetRequiredService<IMovementRepository>();

                var movement = new Movement
                {
                    Id = Guid.NewGuid(),
                    AccountId = message.AccountId,
                    Type = "D",
                    Value = message.Value,
                    CreatedAt = DateTime.UtcNow
                };

                await movementRepository.CreateMovementAsync(movement);

                _logger.LogInformation("[Kafka] Tarifa debitada da conta {AccountId}: {Value}", message.AccountId, message.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar tarifa via Kafka para a conta {AccountId}", message.AccountId);
                throw;
            }
        }
    }
}

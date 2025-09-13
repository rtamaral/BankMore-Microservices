using BankMore.Domain.Entities;
using BankMore.Infrastructure.Repositories;
using KafkaFlow;
using KafkaFlow.Producers;

namespace BankMore.Api.Infrastructure.Messaging
{
    public class TarifaHandler : IMessageHandler<TransferMessage>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IProducerAccessor _producerAccessor;
        private readonly decimal _tarifaValue = 2m;
        private const string TarifaProducerName = "tarifa-producer";

        public TarifaHandler(IServiceProvider serviceProvider, IProducerAccessor producerAccessor)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _producerAccessor = producerAccessor ?? throw new ArgumentNullException(nameof(producerAccessor));
        }

        public async Task Handle(IMessageContext context, TransferMessage message)
        {
            // Criar scope para resolver serviços Scoped
            using (var scope = _serviceProvider.CreateScope())
            {
                var movementRepository = scope.ServiceProvider.GetRequiredService<IMovementRepository>();

                // Debitar a tarifa
                var movement = new Movement
                {
                    Id = Guid.NewGuid(),
                    AccountId = message.AccountId,
                    Type = "D",
                    Value = _tarifaValue,
                    CreatedAt = DateTime.UtcNow
                };

                await movementRepository.CreateMovementAsync(movement);

                // Publicar evento de tarifa no Kafka
                var producer = _producerAccessor.GetProducer(TarifaProducerName);
                await producer.ProduceAsync(
                    "tarifas-realizadas",
                    message.AccountId.ToString(),
                    new { AccountId = message.AccountId, Value = _tarifaValue }
                );
            }
        }
    }

    public record TransferMessage(Guid RequestId, Guid AccountId, decimal Value);
}

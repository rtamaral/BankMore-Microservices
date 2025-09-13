using BankMore.Api.Application.Kafka.Messages;
using KafkaFlow.Producers;
using Microsoft.Extensions.Logging;

namespace BankMore.Api.Infrastructure.Messaging
{
    public class TransferKafkaProducer : ITransferKafkaProducer
    {
        private readonly IProducerAccessor _producerAccessor;
        private readonly ILogger<TransferKafkaProducer> _logger;
        private const string ProducerName = "transferProducer";

        public TransferKafkaProducer(IProducerAccessor producerAccessor, ILogger<TransferKafkaProducer> logger)
        {
            _producerAccessor = producerAccessor ?? throw new ArgumentNullException(nameof(producerAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task PublishTransferAsync(Guid requestId, Guid sourceAccountId, Guid destinationAccountId, decimal value)
        {
            try
            {
                var message = new TransferKafkaMessage
                {
                    RequestId = requestId,
                    SourceAccountId = sourceAccountId,
                    DestinationAccountId = destinationAccountId,
                    Value = value,
                    Date = DateTime.UtcNow
                };

                var producer = _producerAccessor.GetProducer(ProducerName);

                if (producer == null)
                {
                    _logger.LogError("Producer '{ProducerName}' não encontrado.", ProducerName);
                    throw new InvalidOperationException($"Producer '{ProducerName}' não registrado.");
                }

                _logger.LogInformation(
                    "Publicando transferência: RequestId={RequestId}, Source={SourceAccountId}, Destination={DestinationAccountId}, Value={Value}",
                    requestId, sourceAccountId, destinationAccountId, value);

                await producer.ProduceAsync("transferencias-realizadas", requestId.ToString(), message);

                _logger.LogInformation("Transferência publicada no Kafka com sucesso: RequestId={RequestId}", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar transferência no Kafka: RequestId={RequestId}", requestId);
                throw;
            }
        }
    }
}

using BankMore.Api.Application.Kafka.Messages;
using KafkaFlow;
using System.Threading.Tasks;


namespace BankMore.Api.Application.Kafka.Handlers
{
    public class TariffMessageHandler : IMessageHandler<TransferKafkaMessage>
    {
        public Task Handle(IMessageContext context, TransferKafkaMessage message)
        {
            // Exemplo simples: aqui você poderia debitar uma tarifa da conta de origem
            Console.WriteLine($"[Kafka] Transferência recebida para cobrança de tarifa:");
            Console.WriteLine($"De {message.SourceAccountId} para {message.DestinationAccountId}, valor {message.Value}");

            return Task.CompletedTask;
        }
    }
}

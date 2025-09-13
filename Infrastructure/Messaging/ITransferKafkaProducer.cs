using System;
using System.Threading.Tasks;

namespace BankMore.Api.Infrastructure.Messaging
{
    public interface ITransferKafkaProducer
    {
        Task PublishTransferAsync(Guid requestId, Guid sourceAccountId, Guid destinationAccountId, decimal value);

    }
}

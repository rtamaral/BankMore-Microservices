using System;

namespace BankMore.Api.Application.Kafka.Messages
{
    public class TransferKafkaMessage
    {
        public Guid SourceAccountId { get; set; }
        public Guid DestinationAccountId { get; set; }
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        public Guid RequestId { get; set; } = Guid.NewGuid();
    }
}

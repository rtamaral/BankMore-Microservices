using MediatR;

namespace BankMore.Application.Commands
{
    public class TransferCommand : IRequest<bool>
    {
        public Guid RequestId { get; set; } // nova propriedade para idempotência
        public Guid SourceAccountId { get; set; }
        public Guid DestinationAccountId { get; set; }
        public decimal Value { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty; // opcional
    }

}

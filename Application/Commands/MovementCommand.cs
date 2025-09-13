using MediatR;

namespace BankMore.Application.Commands
{
    public class MovementCommand : IRequest<bool>
    {
        public Guid AccountId { get; set; }
        public decimal Value { get; set; }
        public string Type { get; set; } = string.Empty; // "C" or "D"
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}

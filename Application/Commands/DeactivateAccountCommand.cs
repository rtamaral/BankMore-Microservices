using MediatR;

namespace BankMore.Api.Application.Commands
{
    public class DeactivateAccountCommand : IRequest<bool>
    {
        public Guid AccountId { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}

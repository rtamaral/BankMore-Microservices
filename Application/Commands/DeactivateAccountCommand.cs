using MediatR;

namespace BankMore.Api.Application.Commands
{
    public class DeactivateAccountCommand : IRequest<bool>
    {
        // Mesmo tipo que o LoginCommand
        public int AccountNumber { get; set; }

        // Senha da conta para validação
        public string Password { get; set; } = string.Empty;
    }
}

using MediatR;
using BankMore.Shared.DTOs;

namespace BankMore.Application.Commands
{
    public class RegisterAccountCommand : IRequest<CreateAccountResultDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

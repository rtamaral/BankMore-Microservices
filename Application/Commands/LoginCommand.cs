using BankMore.Shared.DTOs;
using MediatR;

namespace BankMore.Application.Commands
{
    public class LoginCommand : IRequest<LoginResultDto>
    {
        public int AccountNumber { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}

using System;

namespace BankMore.Api.Application.Shared.Exceptions
{
    public class UnauthorizedUserException : Exception
    {
        public UnauthorizedUserException()
            : base("Usuário não autorizado.") { }

        public UnauthorizedUserException(string message)
            : base(message) { }

        public UnauthorizedUserException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

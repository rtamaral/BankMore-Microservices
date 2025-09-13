using System;

namespace BankMore.Api.Application.Shared.Exceptions
{
    public class InactiveAccountException : Exception
    {
        public InactiveAccountException()
            : base("Conta inativa.") { }

        public InactiveAccountException(string message)
            : base(message) { }

        public InactiveAccountException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

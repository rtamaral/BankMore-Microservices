using System;

namespace BankMore.Api.Application.Shared.Exceptions
{
    public class InvalidAccountException : Exception
    {
        public InvalidAccountException()
            : base("Conta inválida.") { }

        public InvalidAccountException(string message)
            : base(message) { }

        public InvalidAccountException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
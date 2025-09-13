using System;

namespace BankMore.Api.Application.Shared.Exceptions
{
    public class InvalidValueException : Exception
    {
        public InvalidValueException()
            : base("Valor inválido.") { }

        public InvalidValueException(string message)
            : base(message) { }

        public InvalidValueException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

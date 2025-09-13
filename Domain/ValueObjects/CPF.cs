using BankMore.Api.Application.Shared.Helpers;

namespace BankMore.Domain.ValueObjects;

public class Cpf
{
    public string Number { get; private set; }

    public Cpf(string number)
    {
        if (!CpfValidator.IsValid(number))
            throw new ArgumentException("Invalid CPF.");
        Number = number;
    }
}

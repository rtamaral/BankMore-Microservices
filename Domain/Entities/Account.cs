namespace BankMore.Domain.Entities;

public class Account
{
    public Guid Id { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int AccountNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
}

namespace BankMore.Api.Application.Shared.DTOs
{
    public class AccountBalanceDto
    {
        public int AccountNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty; // novo
        public decimal Balance { get; set; }
        public DateTime Date { get; set; }

        public AccountBalanceDto(int accountNumber, string name, string cpf, decimal balance, DateTime date)
        {
            AccountNumber = accountNumber;
            Name = name;
            Cpf = cpf;
            Balance = balance;
            Date = date;
        }
    }
}

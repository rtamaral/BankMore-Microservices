namespace BankMore.Shared.DTOs
{
    public class LoginResultDto
    {
        public string Token { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int AccountNumber { get; set; }

        public LoginResultDto(string token, string name, int accountNumber)
        {
            Token = token;
            Name = name;
            AccountNumber = accountNumber;
        }
    }
}

namespace BankMore.Shared.DTOs
{
    public class CreateAccountResultDto
    {
        public Guid AccountId { get; set; }
        public int AccountNumber { get; set; }

        public CreateAccountResultDto(Guid accountId, int accountNumber)
        {
            AccountId = accountId;
            AccountNumber = accountNumber;
        }
    }
}

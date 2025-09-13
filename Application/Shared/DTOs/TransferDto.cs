namespace BankMore.Api.Application.Shared.DTOs
{
    public class TransferDto
    {
        public Guid TransferId { get; set; }
        public Guid SourceAccountId { get; set; }
        public Guid DestinationAccountId { get; set; }
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
    }
}

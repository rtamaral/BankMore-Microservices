namespace BankMore.Domain.Entities
{
    public class Transfer
    {
        public Guid Id { get; set; }
        public Guid SourceAccountId { get; set; }
        public Guid DestinationAccountId { get; set; }
        public decimal Value { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}

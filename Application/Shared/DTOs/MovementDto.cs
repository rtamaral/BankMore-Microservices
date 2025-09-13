namespace BankMore.Api.Application.Shared.DTOs
{
    public class MovementDto
    {
        public Guid MovementId { get; set; }
        public Guid AccountId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
    }
}

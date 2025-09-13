namespace BankMore.Domain.Entities;

public class Movement
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public decimal Value { get; set; }
    public string Type { get; set; } = string.Empty; // CREDIT / DEBIT
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

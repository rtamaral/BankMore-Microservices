namespace BankMore.Api.Application.Shared.DTOs
{
    public class MovementHistoryDto
    {
        public int AccountNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<MovementDto> Movements { get; set; } = new();

        public MovementHistoryDto(int accountNumber, string name, List<MovementDto> movements)
        {
            AccountNumber = accountNumber;
            Name = name;
            Movements = movements;
        }
    }
}

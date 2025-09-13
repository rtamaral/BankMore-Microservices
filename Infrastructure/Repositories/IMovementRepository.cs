using BankMore.Domain.Entities;

namespace BankMore.Infrastructure.Repositories
{
    public interface IMovementRepository
    {
        Task<int> CreateMovementAsync(Movement movement);
        Task<decimal> GetBalanceAsync(Guid accountId);
    }
}

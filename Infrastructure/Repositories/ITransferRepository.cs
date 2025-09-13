using BankMore.Domain.Entities;

namespace BankMore.Infrastructure.Repositories
{
    public interface ITransferRepository
    {
        Task<int> CreateTransferAsync(Transfer transfer);
    }
}

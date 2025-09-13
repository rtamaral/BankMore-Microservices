using BankMore.Domain.Entities;

namespace BankMore.Infrastructure.Repositories
{
    public interface IAccountRepository
    {
        Task<Account?> GetByIdAsync(Guid accountId);
        Task<Account?> GetByNumberAsync(int accountNumber);
        Task<Account?> GetByCpfAsync(string cpf);
        Task<int> CreateAsync(Account account);
        Task<bool> UpdateStatusAsync(Guid accountId, bool active);
    }
}

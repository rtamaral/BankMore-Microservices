using BankMore.Api.Domain.Entities;
using System.Threading.Tasks;

namespace BankMore.Infrastructure.Repositories
{
    public interface ITariffRepository
    {
        Task CreateTariffAsync(Tariff tariff);
    }
}

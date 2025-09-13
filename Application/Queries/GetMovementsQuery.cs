using BankMore.Api.Application.Shared.DTOs;
using MediatR;

namespace BankMore.Api.Application.Queries
{
    public class GetMovementsQuery : IRequest<MovementHistoryDto>
    {
        public Guid AccountId { get; }

        public GetMovementsQuery(Guid accountId)
        {
            AccountId = accountId;
        }
    }
}

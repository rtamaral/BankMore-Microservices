namespace BankMore.Application.Queries
{
    using BankMore.Api.Application.Shared.DTOs;
    using MediatR;
    using System;

    public class GetBalanceQuery : IRequest<AccountBalanceDto>
    {
        public Guid AccountId { get; }

        public GetBalanceQuery(Guid accountId)
        {
            AccountId = accountId;
        }
    }
}

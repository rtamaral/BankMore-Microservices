using BankMore.Application.Commands;
using Swashbuckle.AspNetCore.Filters;

namespace BankMore.Api.SwaggerExamples
{
    public class TransferCommandExample : IExamplesProvider<TransferCommand>
    {
        public TransferCommand GetExamples()
        {
            return new TransferCommand
            {
                RequestId = Guid.Parse("3fa85f64-5767-4562-9abc-2c963f66afa6"),
                DestinationAccountId = Guid.Parse("c1d63f1c-e591-4314-9f2b-7ee7d11f3d11"),
                Value = 13,
                IdempotencyKey = "d9b2d63d-a233-4123-847a-6c2f7b8e9f17"
            };
        }
    }
}

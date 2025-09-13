using BankMore.Api.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace BankMore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KafkaTestController : ControllerBase
    {
        private readonly ITransferKafkaProducer _producer;

        public KafkaTestController(ITransferKafkaProducer producer)
        {
            _producer = producer;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendTestMessage(Guid sourceAccountId, Guid destinationAccountId, decimal value)
        {
            var requestId = Guid.NewGuid();

            await _producer.PublishTransferAsync(requestId, sourceAccountId, destinationAccountId, value);

            return Ok(new
            {
                Message = "Mensagem enviada para o Kafka",
                RequestId = requestId,
                SourceAccountId = sourceAccountId,
                DestinationAccountId = destinationAccountId,
                Value = value
            });
        }

    }
}

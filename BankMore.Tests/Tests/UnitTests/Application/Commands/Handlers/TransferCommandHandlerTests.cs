using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using BankMore.Application.Commands;
using BankMore.Application.Commands.Handlers;
using BankMore.Infrastructure.Repositories;
using BankMore.Domain.Entities;
using BankMore.Api.Infrastructure.Messaging;

namespace BankMore.Tests.UnitTests.Application.Commands.Handlers
{
    public class TransferCommandHandlerTests
    {
        private readonly Guid sourceAccountId = Guid.NewGuid();
        private readonly Guid destinationAccountId = Guid.NewGuid();
        private readonly Guid idempotencyKey = Guid.NewGuid();

        [Fact]
        public async Task Handle_Should_Process_Transfer_When_Not_Exists()
        {
            // Arrange
            var movementRepoMock = new Mock<IMovementRepository>();
            movementRepoMock.Setup(m => m.CreateMovementAsync(It.IsAny<Movement>()))
                .ReturnsAsync(1);

            var transferRepoMock = new Mock<ITransferRepository>();

            var kafkaProducerMock = new Mock<ITransferKafkaProducer>();
            kafkaProducerMock.Setup(k => k.PublishTransferAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<decimal>()))
                .Returns(Task.CompletedTask);

            var dbConnectionMock = new Mock<IDbConnection>();
            // Aqui supomos que seu handler usa Dapper; se usar Moq.Dapper, adicione as chamadas SetupDapperAsync apropriadas.
            // Se preferir, você pode injetar um IDbConnection real apontando para um DB de teste, ou um stub que implemente as chamadas necessárias.

            var loggerMock = new Mock<ILogger<TransferCommandHandler>>();

            var handler = new TransferCommandHandler(
                Mock.Of<IAccountRepository>(),    // account repo (pode ser Mock.Of)
                movementRepoMock.Object,
                dbConnectionMock.Object,
                kafkaProducerMock.Object,
                loggerMock.Object
            );

            var command = new TransferCommand
            {
                SourceAccountId = sourceAccountId,
                DestinationAccountId = destinationAccountId,
                Value = 100m,
                IdempotencyKey = idempotencyKey.ToString()
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert (dependendo do que o handler faz com DB real ou fake, esses asserts podem variar)
            Assert.True(result);
            movementRepoMock.Verify(m => m.CreateMovementAsync(It.IsAny<Movement>()), Times.Exactly(2));
            kafkaProducerMock.Verify(k => k.PublishTransferAsync(
                It.IsAny<Guid>(), sourceAccountId, destinationAccountId, 100m), Times.Once);
        }

        // Outros testes (ex.: já processada, saldo insuficiente, validações) seguem a mesma linha.
    }
}
using BankMore.Api.Domain.Entities;
using BankMore.Api.Infrastructure.Messaging;
using BankMore.Application.Commands;
using BankMore.Application.Commands.Handlers;
using BankMore.Domain.Entities;
using BankMore.Infrastructure.Repositories;
using Dapper;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Dapper;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BankMore.Tests.UnitTests.Application.Commands.Handlers
{
    public class TransferCommandHandlerTests
    {
        private readonly Mock<IMovementRepository> _movementRepositoryMock = new();
        private readonly Mock<ITariffRepository> _tariffRepositoryMock = new();
        private readonly Mock<IDbConnection> _dbConnectionMock = new();
        private readonly Mock<ILogger<TransferCommandHandler>> _loggerMock = new();
        private readonly Mock<ITransferKafkaProducer> _kafkaProducerMock = new();

        private TransferCommandHandler CreateHandler()
        {
            return new TransferCommandHandler(
                _dbConnectionMock.Object,
                _movementRepositoryMock.Object,
                _tariffRepositoryMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldProcessTransferAndTariff_WhenValidRequest()
        {
            // Arrange
            var request = new TransferCommand
            {
                SourceAccountId = Guid.NewGuid(),
                DestinationAccountId = Guid.NewGuid(),
                Value = 100m,
                IdempotencyKey = Guid.NewGuid().ToString()
            };

            decimal initialBalance = 200m;
            decimal tariffValue = 2m; // Tarifa fixa

            // Mock saldo
            _dbConnectionMock.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<decimal>(
                It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(initialBalance);

            // Mock idempotência inexistente
            _dbConnectionMock.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<Guid?>(
                It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync((Guid?)null);

            _dbConnectionMock.SetupDapperAsync(c => c.ExecuteAsync(
                It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(1);

            _dbConnectionMock.SetupDapperAsync(c => c.ExecuteAsync(
                It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(1); 


            _tariffRepositoryMock.Setup(t => t.CreateTariffAsync(It.IsAny<Tariff>()))
                .Returns(Task.CompletedTask);

            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.True(result);

            // Movimentos de transferência
            _movementRepositoryMock.Verify(m => m.CreateMovementAsync(It.Is<Movement>(mov =>
                mov.AccountId == request.SourceAccountId && mov.Type == "D" && mov.Value == request.Value)), Times.Once);

            _movementRepositoryMock.Verify(m => m.CreateMovementAsync(It.Is<Movement>(mov =>
                mov.AccountId == request.DestinationAccountId && mov.Type == "C" && mov.Value == request.Value)), Times.Once);

            // Movimentos de tarifa
            _movementRepositoryMock.Verify(m => m.CreateMovementAsync(It.Is<Movement>(mov =>
                mov.AccountId == request.SourceAccountId && mov.Type == "D" && mov.Value == tariffValue)), Times.Once);

            // Tarifa registrada
            _tariffRepositoryMock.Verify(t => t.CreateTariffAsync(It.Is<Tariff>(tar =>
                tar.IdContaCorrente == request.SourceAccountId && tar.Valor == tariffValue)), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenIdempotencyKeyExists()
        {
            // Arrange
            var idempotencyKey = Guid.NewGuid();
            var request = new TransferCommand
            {
                SourceAccountId = Guid.NewGuid(),
                DestinationAccountId = Guid.NewGuid(),
                Value = 100m,
                IdempotencyKey = idempotencyKey.ToString()
            };

            _dbConnectionMock.SetupDapperAsync(c => c.QueryFirstOrDefaultAsync<Guid?>(
                It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(idempotencyKey);

            var handler = CreateHandler();

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.False(result);
        }
    }
}

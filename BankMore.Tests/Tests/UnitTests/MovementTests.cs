using BankMore.Api.Application.Commands.Handlers;
using BankMore.Api.Application.Queries;
using BankMore.Api.Application.Shared.DTOs;
using BankMore.Infrastructure.Database;
using System.Data;
using Dapper;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;

namespace BankMore.Tests.UnitTests.UnitTests
{
    public class MovementTests
    {
        [Fact]
        public async Task GetMovementsHandler_Returns_MovementHistory()
        {
            // Arrange
            var accountId = Guid.NewGuid();

            var connectionMock = new Mock<IDbConnection>();
            var factoryMock = new Mock<SqlServerConnectionFactory>("fake-connection-string");

            // Mock ILogger
            var loggerMock = new Mock<ILogger<GetMovementsHandler>>();

            // Mock para retornar conta usando QueryFirstOrDefaultAsync
            connectionMock
                .Setup(c => c.QueryFirstOrDefaultAsync<dynamic>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null, null, null
                ))
                .ReturnsAsync(new { numero = 1234, nome = "Test User" });

            // Mock para retornar movimentações usando QueryAsync
            connectionMock
                .Setup(c => c.QueryAsync<MovementDto>(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null, null, null
                ))
                .ReturnsAsync(new List<MovementDto>
                {
                    new MovementDto { Value = 100, Date = DateTime.UtcNow, Type = "CREDITO" },
                    new MovementDto { Value = 50, Date = DateTime.UtcNow, Type = "DEBITO" }
                });

            // Configura para retornar o mock de conexão
            factoryMock.Setup(f => f.CreateConnection()).Returns(connectionMock.Object);

            // Instancia handler com factory e logger
            var handler = new GetMovementsHandler(factoryMock.Object, loggerMock.Object);
            var query = new GetMovementsQuery(accountId);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1234, result.AccountNumber);
            Assert.Equal("Test User", result.Name);
            Assert.Equal(2, result.Movements.Count);
            Assert.Equal(100, result.Movements[0].Value);
            Assert.Equal(50, result.Movements[1].Value);
        }
    }
}

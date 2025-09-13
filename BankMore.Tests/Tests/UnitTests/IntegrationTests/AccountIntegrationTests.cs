using BankMore;
using BankMore.Shared.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
namespace BankMore.Tests.UnitTests.UnitTests.IntegrationTests
{
    public class AccountIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public AccountIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task RegisterAccount_ReturnsSuccess_WhenValid()
        {
            // Arrange
            var payload = new
            {
                Name = "Test User",
                Cpf = "12345678901",
                Password = "StrongPassword123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/account/register", payload);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CreateAccountResultDto>();

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result!.AccountId);
            Assert.True(result.AccountNumber > 0);
        }

        [Fact]
        public async Task RegisterAccount_ReturnsBadRequest_WhenCpfInvalid()
        {
            // Arrange
            var payload = new
            {
                Name = "Test User",
                Cpf = "123", // CPF inválido
                Password = "StrongPassword123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/account/register", payload);

            // Assert
            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

            var error = await response.Content.ReadFromJsonAsync<ErrorResponseFake>();
            Assert.NotNull(error);
            Assert.Equal("INVALID_DOCUMENT", error!.errorType);
        }

        private class ErrorResponseFake
        {
            public string message { get; set; } = string.Empty;
            public string errorType { get; set; } = string.Empty;
        }
    }
}

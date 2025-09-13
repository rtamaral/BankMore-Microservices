using BankMore.Api.Application.Shared.DTOs;
using BankMore.Shared.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace BankMore.Api.Tests.UnitTests.UnitTests.IntegrationTests
{
    public class LoginIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public LoginIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Login_ReturnsToken_WhenCredentialsAreValid()
        {
            // Arrange
            var loginPayload = new
            {
                AccountNumber = 1234, // Use uma conta válida do DB de teste
                Password = "123456"   // Senha correspondente (hash gerado no DB)
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/account/login", loginPayload);

            // Assert
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LoginResultDto>();
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result!.Token));
            Assert.Equal(1234, result.AccountNumber);
            Assert.NotEmpty(result.Name);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
        {
            // Arrange
            var loginPayload = new
            {
                AccountNumber = 1234,
                Password = "wrong-password"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/account/login", loginPayload);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}

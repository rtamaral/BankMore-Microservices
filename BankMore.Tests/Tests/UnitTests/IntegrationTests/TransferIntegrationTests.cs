using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using Xunit;
using System.Net.Http.Json;
using BankMore;
using BankMore.Api.Application.Shared.DTOs;

namespace BankMore.Tests.UnitTests.UnitTests.IntegrationTests
{
    public class TransferIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public TransferIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Transfer_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange: login da conta de origem
            var loginPayload = new
            {
                AccountNumber = 100001,
                Password = "Password123"
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/account/login", loginPayload);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseFake>();
            Assert.NotNull(loginResult?.Token);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token);

            var transferPayload = new
            {
                SourceAccountId = Guid.NewGuid(),  
                DestinationAccountId = Guid.NewGuid(),  
                Value = 50m
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/transfer", transferPayload);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Transfer_ReturnsBadRequest_WhenInsufficientBalance()
        {
            // Arrange: login da conta de origem
            var loginPayload = new
            {
                AccountNumber = 100001,
                Password = "Password123"
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/account/login", loginPayload);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseFake>();
            Assert.NotNull(loginResult?.Token);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token);

            var transferPayload = new
            {
                SourceAccountId = Guid.NewGuid(),   
                DestinationAccountId = Guid.NewGuid(),  
                Value = 1000000m // valor maior que o saldo
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/transfer", transferPayload);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

            var error = await response.Content.ReadFromJsonAsync<ErrorResponseFake>();
            Assert.NotNull(error);
            Assert.Equal("INSUFFICIENT_BALANCE", error!.errorType);
        }

        private class LoginResponseFake
        {
            public string Token { get; set; } = string.Empty;
        }

        private class ErrorResponseFake
        {
            public string message { get; set; } = string.Empty;
            public string errorType { get; set; } = string.Empty;
        }
    }
}

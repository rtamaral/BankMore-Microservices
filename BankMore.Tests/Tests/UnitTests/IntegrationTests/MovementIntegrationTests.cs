using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BankMore.Api.Tests.UnitTests.UnitTests.IntegrationTests
{
    public class MovementIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public MovementIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetHistory_ReturnsMovements_WhenAuthenticated()
        {
            // Arrange: login para pegar token
            var loginPayload = new
            {
                AccountNumber = 1234, // Use uma conta válida do seu DB de teste
                Password = "123456"   // Senha correspondente
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/account/login", loginPayload);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseFake>();
            Assert.NotNull(loginResult?.Token);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token);

            // Act: chama o endpoint de extrato
            var response = await _client.GetAsync("/api/movement/history");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var history = JsonSerializer.Deserialize<MovementHistoryFake>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert
            Assert.NotNull(history);
            Assert.Equal(1234, history!.AccountNumber);
            Assert.NotEmpty(history.Movements);
        }

        private class LoginResponseFake
        {
            public string Token { get; set; } = string.Empty;
        }

        private class MovementHistoryFake
        {
            public int AccountNumber { get; set; }
            public string Name { get; set; } = string.Empty;
            public List<MovementDtoFake> Movements { get; set; } = new();
        }

        private class MovementDtoFake
        {
            public decimal Amount { get; set; }
            public DateTime Date { get; set; }
            public string Type { get; set; } = string.Empty;
        }
    }
}

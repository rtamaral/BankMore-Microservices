using System.Security.Claims;

namespace BankMore.Api.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid? GetAccountId(this ClaimsPrincipal user)
        {
            // Tenta buscar por diferentes nomes de claim
            var accountIdString = user.FindFirst("AccountId")?.Value
                                ?? user.FindFirst("idConta")?.Value
                                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(accountIdString, out var accountId) ? accountId : null;
        }

        public static string? GetAccountNumber(this ClaimsPrincipal user)
        {
            return user.FindFirst("AccountNumber")?.Value
                ?? user.FindFirst("numero")?.Value;
        }

        public static string? GetUserName(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value;
        }
    }
}

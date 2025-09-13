using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BankMore.Services
{
    public interface IJwtService
    {
        string GenerateToken(string accountId, string accountNumber, string name);
        ClaimsPrincipal? ValidateToken(string token);
    }

    public class JwtService : IJwtService
    {
        private readonly string _secret;
        private readonly int _expiryMinutes;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
        {
            _secret = configuration["Jwt:Secret"]
                      ?? throw new ArgumentNullException("Jwt:Secret not configured");
            _expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out int minutes) ? minutes : 60;
            _logger = logger;
        }

        public string GenerateToken(string accountId, string accountNumber, string name)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                // Evita renomear claims customizadas (ex: "AccountId" → "nameidentifier")
                JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

                var key = Encoding.UTF8.GetBytes(_secret);

                var claims = new List<Claim>
                {
                    new Claim("idConta", accountId),
                    new Claim("numero", accountNumber),
                    new Claim(ClaimTypes.Name, name)
                };


                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(_expiryMinutes),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                string jwt = tokenHandler.WriteToken(token);

                _logger.LogInformation("JWT gerado para AccountId: {AccountId}, expira em {Expiry}",
                    accountId, tokenDescriptor.Expires);

                return jwt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar JWT para AccountId: {AccountId}", accountId);
                throw;
            }
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Tentativa de validar token vazio ou nulo.");
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secret);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                _logger.LogInformation("JWT validado com sucesso. Expira em {Expiry}",
                    validatedToken.ValidTo);

                return principal;
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning(ex, "Token expirado em {ExpiredAt}", ex.Expires);
                return null;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Falha na validação do token.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao validar token.");
                return null;
            }
        }
    }
}

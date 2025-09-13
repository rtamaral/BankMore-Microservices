using BankMore.Infrastructure.Database;
using BankMore.Services;
using BankMore.Shared.DTOs;
using Dapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace BankMore.Application.Commands.Handlers
{
    public class LoginHandler : IRequestHandler<LoginCommand, LoginResultDto>
    {
        private readonly SqlServerConnectionFactory _connectionFactory;
        private readonly IJwtService _jwtService;
        private readonly ILogger<LoginHandler> _logger;

        public LoginHandler(
            SqlServerConnectionFactory connectionFactory,
            IJwtService jwtService,
            ILogger<LoginHandler> logger)
        {
            _connectionFactory = connectionFactory;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                using IDbConnection db = _connectionFactory.CreateConnection();

                // Consulta a conta pelo número
                var account = await db.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT idcontacorrente, numero, nome, senha, salt, ativo FROM contacorrente WHERE numero = @Numero",
                    new { Numero = request.AccountNumber }
                );

                if (account == null || account.ativo != true)
                {
                    _logger.LogWarning(
                        "Tentativa de login falhou: conta não encontrada ou inativa. Número da conta: {AccountNumber}",
                        request.AccountNumber);
                    throw new UnauthorizedAccessException("Account not found or inactive");
                }

                // Valida a senha usando o hash e salt armazenados
                if (!VerifyPassword(request.Password, (string)account.senha, (string)account.salt))
                {
                    _logger.LogWarning(
                        "Tentativa de login falhou: senha inválida. Número da conta: {AccountNumber}",
                        request.AccountNumber);
                    throw new UnauthorizedAccessException("Invalid password");
                }

                // Gera token JWT
                string token = _jwtService.GenerateToken(
                    account.idcontacorrente.ToString(),
                    account.numero.ToString(),
                    account.nome.ToString()
                );

                _logger.LogInformation(
                    "Login realizado com sucesso. Número da conta: {AccountNumber}",
                    request.AccountNumber);

                return new LoginResultDto(token, account.nome, account.numero);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Falha de autenticação para a conta {AccountNumber}", request.AccountNumber);
                throw;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Erro ao acessar o banco de dados durante login da conta {AccountNumber}", request.AccountNumber);
                throw new InvalidOperationException("Erro ao acessar o banco de dados.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao realizar login da conta {AccountNumber}", request.AccountNumber);
                throw new InvalidOperationException("Erro inesperado ao realizar login.", ex);
            }
        }

        [ApiController]
        [Route("api/[controller]")]
        public class DebugController : ControllerBase
        {
            private readonly IJwtService _jwtService;
            private readonly ILogger<DebugController> _logger;

            public DebugController(IJwtService jwtService, ILogger<DebugController> logger)
            {
                _jwtService = jwtService;
                _logger = logger;
            }

            [HttpPost("decode-token")]
            public IActionResult DecodeToken([FromBody] string token)
            {
                try
                {
                    if (string.IsNullOrEmpty(token))
                    {
                        return BadRequest("Token não fornecido");
                    }

                    // Remove "Bearer " se estiver presente
                    if (token.StartsWith("Bearer "))
                    {
                        token = token.Substring(7);
                    }

                    _logger.LogInformation("Tentando decodificar token...");

                    // Usar seu JwtService para validar
                    var principal = _jwtService.ValidateToken(token);

                    if (principal == null)
                    {
                        _logger.LogWarning("Token inválido ou expirado");
                        return BadRequest(new { error = "Token inválido ou expirado" });
                    }

                    var claims = principal.Claims.Select(c => new
                    {
                        Type = c.Type,
                        Value = c.Value
                    }).ToList();

                    _logger.LogInformation("Token decodificado com sucesso. Claims encontradas: {ClaimsCount}", claims.Count);

                    return Ok(new
                    {
                        valid = true,
                        claims = claims,
                        identity = principal.Identity?.Name,
                        isAuthenticated = principal.Identity?.IsAuthenticated
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao decodificar token");
                    return BadRequest(new { error = ex.Message });
                }
            }

            // Endpoint para testar se a API está respondendo
            [HttpGet("ping")]
            public IActionResult Ping()
            {
                return Ok(new { message = "API está funcionando", timestamp = DateTime.UtcNow });
            }
        }

        /// <summary>
        /// Verifica se a senha fornecida corresponde ao valor armazenado
        /// </summary>
        /// <param name="password">Senha ou hash fornecido pelo cliente</param>
        /// <param name="storedHash">Hash da senha armazenado no banco de dados</param>
        /// <param name="storedSalt">Salt armazenado no banco de dados</param>
        /// <returns>True se a senha for válida, false caso contrário</returns>
        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            try
            {
                // DEBUG: Mostrar os valores recebidos
                _logger.LogInformation("=== DEBUG VerifyPassword ===");
                _logger.LogInformation("Password recebido: '{Password}' (Length: {Length})", password, password.Length);
                _logger.LogInformation("StoredHash do banco: '{StoredHash}' (Length: {Length})", storedHash, storedHash.Length);
                _logger.LogInformation("StoredSalt do banco: '{StoredSalt}' (Length: {Length})", storedSalt, storedSalt?.Length ?? 0);

                //Comparação direta (caso o cliente envie o hash já processado)
                bool directMatch = password == storedHash;
                _logger.LogInformation("1. Comparação direta: {DirectMatch}", directMatch);

                if (directMatch)
                {
                    _logger.LogInformation("=== SUCESSO: Match direto ===");
                    return true;
                }

                //Tentar decodificar o storedHash para ver seu conteúdo
                if (!string.IsNullOrEmpty(storedHash))
                {
                    try
                    {
                        byte[] decodedHashBytes = Convert.FromBase64String(storedHash);
                        string decodedHashString = Encoding.UTF8.GetString(decodedHashBytes);
                        _logger.LogInformation("2. Hash decodificado: '{DecodedHash}' (Length: {Length})", decodedHashString, decodedHashString.Length);

                        // Comparar com a senha original
                        bool passwordMatchDecoded = password == decodedHashString;
                        _logger.LogInformation("2a. Password == Hash decodificado: {Match}", passwordMatchDecoded);

                        if (passwordMatchDecoded)
                        {
                            _logger.LogInformation("=== SUCESSO: Password == Hash decodificado ===");
                            return true;
                        }

                        // Se temos salt, tentar diferentes combinações
                        if (!string.IsNullOrEmpty(storedSalt))
                        {
                            // Decodificar o salt também
                            byte[] decodedSaltBytes = Convert.FromBase64String(storedSalt);
                            string decodedSaltString = Encoding.UTF8.GetString(decodedSaltBytes);
                            _logger.LogInformation("2b. Salt decodificado: '{DecodedSalt}' (Length: {Length})", decodedSaltString, decodedSaltString.Length);

                            // Testar: password + salt decodificado == hash decodificado
                            string combinedDecoded = password + decodedSaltString;
                            bool combinedMatch = combinedDecoded == decodedHashString;
                            _logger.LogInformation("2c. (Password + Salt decodificado) == Hash decodificado: '{Combined}' == '{DecodedHash}': {Match}",
                                combinedDecoded, decodedHashString, combinedMatch);

                            if (combinedMatch)
                            {
                                _logger.LogInformation("=== SUCESSO: Combined match com salt decodificado ===");
                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Erro ao decodificar Base64: {Error}", ex.Message);
                    }
                }

                _logger.LogInformation("=== FALHA: Nenhuma comparação deu match ===");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar senha");
                return false;
            }
        }
    }
}
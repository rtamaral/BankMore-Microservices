using BankMore.Infrastructure.Database;
using BankMore.Shared.DTOs;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace BankMore.Application.Commands.Handlers
{
    public class RegisterAccountHandler : IRequestHandler<RegisterAccountCommand, CreateAccountResultDto>
    {
        private readonly SqlServerConnectionFactory _connectionFactory;
        private readonly ILogger<RegisterAccountHandler> _logger;

        public RegisterAccountHandler(SqlServerConnectionFactory connectionFactory, ILogger<RegisterAccountHandler> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<CreateAccountResultDto> Handle(RegisterAccountCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Cpf) || request.Cpf.Length != 11)
                {
                    _logger.LogWarning("CPF inválido informado: {Cpf}", request.Cpf);
                    throw new ArgumentException("CPF inválido");
                }

                using IDbConnection db = _connectionFactory.CreateConnection();

                // Gera salt aleatório em bytes
                byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
                string salt = Convert.ToBase64String(saltBytes);

                // Gera hash SHA256 + salt
                string senhaHash = GenerateHash(request.Password, saltBytes);

                Guid idConta = Guid.NewGuid();
                int numeroConta = new Random().Next(100000, 999999);

                string sql = @"INSERT INTO contacorrente 
                               (idcontacorrente, numero, nome, ativo, senha, salt, cpf)
                               VALUES (@Id, @Numero, @Nome, 1, @Senha, @Salt, @Cpf)";

                await db.ExecuteAsync(sql, new
                {
                    Id = idConta,
                    Numero = numeroConta,
                    Nome = request.Name,
                    Senha = senhaHash,
                    Salt = salt,
                    Cpf = request.Cpf
                });

                _logger.LogInformation("Conta criada com sucesso. Id: {Id}, Numero: {Numero}", idConta, numeroConta);

                return new CreateAccountResultDto(idConta, numeroConta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar conta para CPF {Cpf}", request.Cpf);
                throw new InvalidOperationException("Erro ao registrar conta.", ex);
            }
        }

        private string GenerateHash(string password, byte[] saltBytes)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] combined = passwordBytes.Concat(saltBytes).ToArray();

            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(combined));
        }
    }
}

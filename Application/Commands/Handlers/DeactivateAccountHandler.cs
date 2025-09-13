using System.Data;
using Dapper;
using MediatR;
using Microsoft.Extensions.Logging;
using BankMore.Infrastructure.Database;

namespace BankMore.Api.Application.Commands.Handlers
{
    public class DeactivateAccountHandler : IRequestHandler<DeactivateAccountCommand, bool>
    {
        private readonly SqlServerConnectionFactory _connectionFactory;
        private readonly ILogger<DeactivateAccountHandler> _logger;

        public DeactivateAccountHandler(SqlServerConnectionFactory connectionFactory, ILogger<DeactivateAccountHandler> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<bool> Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();

            // 1. Verifica se a conta existe e está ativa
            var conta = await connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT Id, Senha, Ativo FROM ContaCorrente WHERE Id = @Id",
                new { Id = request.AccountId });

            if (conta == null || conta.Ativo == 0)
                throw new ArgumentException("Conta inválida ou já inativa.", "INVALID_ACCOUNT");

            // 2. Valida a senha
            if (conta.Senha != request.Password) // aqui você pode aplicar hash/salt se estiver implementado
                throw new UnauthorizedAccessException("Senha incorreta.");

            // 3. Atualiza para inativo
            var rows = await connection.ExecuteAsync(
                "UPDATE ContaCorrente SET Ativo = 0 WHERE Id = @Id",
                new { Id = request.AccountId });

            return rows > 0;
        }
    }
}

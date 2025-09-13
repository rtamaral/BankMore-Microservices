using BankMore.Api.Application.Commands;
using BankMore.Application.Commands;
using BankMore.Infrastructure.Database;
using Dapper;
using MediatR;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace BankMore.Application.Commands.Handlers
{
    public class DeactivateAccountHandler : IRequestHandler<DeactivateAccountCommand, bool>
    {
        private readonly SqlServerConnectionFactory _connectionFactory;
        private readonly IMediator _mediator;

        public DeactivateAccountHandler(SqlServerConnectionFactory connectionFactory, IMediator mediator)
        {
            _connectionFactory = connectionFactory;
            _mediator = mediator;
        }

        public async Task<bool> Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
        {
            // Reaproveita LoginHandler para validar senha
            var loginResult = await _mediator.Send(new LoginCommand
            {
                AccountNumber = request.AccountNumber,
                Password = request.Password
            });

            if (loginResult == null)
                throw new UnauthorizedAccessException("Senha inválida.");

            using IDbConnection conn = _connectionFactory.CreateConnection();
            var sql = "UPDATE contacorrente SET ativo = 0 WHERE numero = @AccountNumber";
            int rows = await conn.ExecuteAsync(sql, new { request.AccountNumber });

            return rows > 0;
        }
    }
}

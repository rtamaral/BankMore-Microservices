using BankMore.Api.Application.Commands;
using BankMore.Api.Application.Queries;
using BankMore.Api.Application.Shared.DTOs;
using BankMore.Application.Commands;
using BankMore.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace BankMore.Api.Controllers
{
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IMediator mediator, ILogger<AccountController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }



        // GET: api/account/balance
        [HttpGet("balance")]
        [ProducesResponseType(typeof(AccountBalanceDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBalance()
        {
            var accountIdClaim = User.FindFirst("AccountId")?.Value;

            if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
                return Unauthorized(new { message = "Conta não encontrada no token." });

            var query = new GetBalanceQuery(accountId);
            var result = await _mediator.Send(query);

            if (result == null)
                return NotFound(new { message = "Conta não encontrada." });

            return Ok(result);
        }

        // GET: api/account/movements
        [HttpGet("movements")]
        [ProducesResponseType(typeof(IEnumerable<MovementHistoryDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetMovements()
        {
            var accountIdClaim = User.FindFirst("AccountId")?.Value;

            if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
                return Unauthorized(new { message = "Conta não encontrada no token." });

            var query = new GetMovementsQuery(accountId);
            var history = await _mediator.Send(query);

            return Ok(history);
        }

        // POST: api/account/movement
        [HttpPost("movement")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreateMovement([FromBody] MovementCommand command)
        {
            try
            {
                var accountIdClaim = User.FindFirst("AccountId")?.Value;
                if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
                    return Unauthorized(new { message = "Conta não encontrada no token." });

                command.AccountId = accountId;

                if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
                    command.IdempotencyKey = Guid.NewGuid().ToString();

                bool result = await _mediator.Send(command);

                if (result)
                    return Ok(new
                    {
                        message = "Movimento realizado com sucesso.",
                        idempotencyKey = command.IdempotencyKey
                    });

                return BadRequest(new { message = "Movimentação já processada ou inválida.", errorType = "DUPLICATE_IDEMPOTENCY_KEY" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erro de validação no movimento. AccountId={AccountId}", command.AccountId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar movimento. AccountId={AccountId}", command.AccountId);
                return StatusCode(500, new { message = "Erro interno ao processar movimento." });
            }
        }

        // POST: api/account/transfer
        [HttpPost("transfer")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> CreateTransfer([FromBody] TransferCommand command)
        {
            try
            {
                var accountIdClaim = User.FindFirst("AccountId")?.Value;
                if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var sourceAccountId))
                    return Unauthorized(new { message = "Conta não encontrada no token." });

                command.SourceAccountId = sourceAccountId;

                await _mediator.Send(command);

                // TODO: Implementar:
                // 1. Persistência na tabela transferencia
                // 2. Estorno automático se falhar crédito no destino

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message, errorType = "INVALID_VALUE" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message, errorType = "FORBIDDEN" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro desconhecido ao criar transferência.");
                return BadRequest(new { message = ex.Message, errorType = "UNKNOWN_ERROR" });
            }
        }

        // POST: api/account/deactivate
        [HttpPost("deactivate")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DeactivateAccount([FromBody] DeactivateAccountCommand command)
        {
            if (command.AccountNumber == 0)
            {
                var claim = User.Claims.FirstOrDefault(c => c.Type == "AccountNumber")?.Value;
                if (!int.TryParse(claim, out int accountNumber))
                    return Forbid("Token inválido ou expirado.");

                command.AccountNumber = accountNumber;
            }

            bool success = await _mediator.Send(command);

            if (success)
                return NoContent();

            return BadRequest(new { message = "Não foi possível inativar a conta." });
        }

        // POST: api/account/register
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterAccountCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetBalance), new { accountId = result.AccountId }, result);
        }

        // POST: api/account/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _mediator.Send(command);

            if (result == null || string.IsNullOrEmpty(result.Token))
                return Unauthorized(new { message = "Usuário ou senha inválidos." });

            return Ok(new { token = result.Token });
        }
    }

    public class DeactivateAccountDto
    {
        /// <summary>
        /// Senha da conta a ser inativada.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}

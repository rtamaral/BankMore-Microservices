using BankMore.Api.Application.Commands;
using BankMore.Api.Application.Queries;
using BankMore.Api.Application.Shared.DTOs;
using BankMore.Api.Extensions;
using BankMore.Application.Commands;
using BankMore.Application.Commands.Handlers;
using BankMore.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
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

        // GET: api/account/movements
        [HttpGet("balance")]
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

                // Se não enviou IdempotencyKey, gera uma nova
                if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
                    command.IdempotencyKey = Guid.NewGuid().ToString();

                bool result = await _mediator.Send(command);

                if (result)
                {
                    return Ok(new
                    {
                        message = "Movimento realizado com sucesso.",
                        idempotencyKey = command.IdempotencyKey
                    });
                }
                else
                {
                    return Ok(new
                    {
                        message = $"A IdempotencyKey '{command.IdempotencyKey}' já foi utilizada em outro movimento. Por favor, forneça uma nova ou deixe em branco para ser gerado automaticamente.",
                        idempotencyKey = command.IdempotencyKey,
                        warning = "DUPLICATE_IDEMPOTENCY_KEY"
                    });
                }
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
        [SwaggerRequestExample(typeof(TransferCommand), typeof(Api.SwaggerExamples.TransferCommandExample))]
        public async Task<IActionResult> CreateTransfer([FromBody] TransferCommand command)
        {
            if (command == null)
                return BadRequest(new { message = "O corpo da requisição é obrigatório." });

            try
            {
                var accountIdClaim = User.FindFirst("AccountId")?.Value;
                if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var sourceAccountId))
                    return Unauthorized(new { message = "Conta não encontrada no token." });

                command.SourceAccountId = sourceAccountId;

                // Gera RequestId e IdempotencyKey se não vierem
                if (command.RequestId == Guid.Empty)
                    command.RequestId = Guid.NewGuid();
                if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
                    command.IdempotencyKey = Guid.NewGuid().ToString();

                bool success = await _mediator.Send(command);

                if (!success)
                {
                    return BadRequest(new
                    {
                        message = "Não foi possível processar a transferência.",
                        errorType = "TRANSFER_FAILED"
                    });
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao criar transferência. SourceAccountId={SourceAccountId}, DestinationAccountId={DestinationAccountId}",
                    command.SourceAccountId, command.DestinationAccountId);
                return BadRequest(new { message = ex.Message, errorType = "INVALID_VALUE" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message, errorType = "FORBIDDEN" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro desconhecido ao criar transferência. SourceAccountId={SourceAccountId}, DestinationAccountId={DestinationAccountId}",
                    command.SourceAccountId, command.DestinationAccountId);
                return StatusCode(500, new { message = "Erro interno ao processar transferência.", errorType = "UNKNOWN_ERROR" });
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
            return CreatedAtAction(nameof(GetMovements), new { accountId = result.AccountId }, result);
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

}

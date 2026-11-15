using Azure;
using System.Security.Claims;
using Duett.API.Dtos;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Duett.API.Controllers;

[ApiController]
[Authorize]
[Route("api/calls")]
public class CallsController : ControllerBase
{
    private readonly ICallingService _callingService;
    private readonly ILogger<CallsController> _logger;

    public CallsController(
        ICallingService callingService,
        ILogger<CallsController> logger)
    {
        _callingService = callingService;
        _logger = logger;
    }

    [HttpPost("matches/{matchId:guid}/token")]
    [EnableRateLimiting("CallingTokens")]
    public async Task<ActionResult<CallingTokenResponseDto>> GetToken(
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var token = await _callingService.GetCallingTokenAsync(
                matchId,
                userId,
                cancellationToken);

            if (token is null)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Matchningen hittades inte.");
            }

            return Ok(token);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "ACS kunde inte utfärda VoIP-token. " +
                "HTTP-status: {Status}, Felkod: {ErrorCode}.",
                ex.Status,
                ex.ErrorCode);

            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Samtalstjänsten är tillfälligt otillgänglig.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "Kunde inte skapa samtalsidentitet.");

            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Samtalsidentiteten kunde inte skapas.");
        }
    }
}
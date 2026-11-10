using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Duett.API.Controllers;

[ApiController]
[Authorize]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private readonly IGameService _service;

    public GamesController(IGameService service)
    {
        _service = service;
    }

    [HttpPost("matches/{matchId:guid}/rounds")]
    public async Task<ActionResult<GameRoundResponseDto>> StartRound(
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _service.StartRoundAsync(
            matchId,
            userId,
            cancellationToken);

        switch (result.Status)
        {
            case StartGameRoundStatus.Success:
                return Ok(result.Round);

            case StartGameRoundStatus.MatchNotFound:
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Matchningen hittades inte.");

            case StartGameRoundStatus.NoQuestionsAvailable:
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Inga frågor finns tillgängliga.",
                    detail:
                        "Det finns inga fler aktiva, ospelade frågor " +
                        "för den här matchningen.");

            case StartGameRoundStatus.GameCompleted:
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Spelet är redan avslutat.",
                    detail:
                        "Båda deltagarna har minst tre rätta gissningar.");

            default:
                throw new InvalidOperationException(
                    "Okänd status för spelstart.");
        }
    }

    [HttpGet("rounds/{roundId:guid}")]
    public async Task<ActionResult<GameRoundResponseDto>> GetRound(
        Guid roundId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var round = await _service.GetRoundAsync(
            roundId,
            userId,
            cancellationToken);

        if (round is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Spelrundan hittades inte.");
        }

        return Ok(round);
    }

    [HttpPost("rounds/{roundId:guid}/submissions")]
    public async Task<IActionResult> Submit(
        Guid roundId,
        [FromBody] SubmitGameRoundDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var status = await _service.SubmitAsync(
            roundId,
            userId,
            dto,
            cancellationToken);

        switch (status)
        {
            case SubmitGameRoundStatus.Success:
                return NoContent();

            case SubmitGameRoundStatus.RoundNotFound:
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Spelrundan hittades inte.");

            case SubmitGameRoundStatus.InvalidOption:
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Ogiltigt svarsalternativ.",
                    detail:
                        "Välj ett svar och en gissning " +
                        "bland rundans alternativ.");

            case SubmitGameRoundStatus.AlreadySubmitted:
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Du har redan skickat in.",
                    detail: "Ditt svar och din gissning är låsta.");

            case SubmitGameRoundStatus.RoundCompleted:
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Rundan är redan avslutad.");

            default:
                throw new InvalidOperationException(
                    "Okänd status för spelinlämning.");
        }
    }

    [HttpGet("rounds/{roundId:guid}/result")]
    public async Task<ActionResult<GameRoundResultDto>> GetResult(
        Guid roundId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _service.GetResultAsync(
            roundId,
            userId,
            cancellationToken);

        switch (result.Status)
        {
            case GameRoundResultStatus.Success:
                return Ok(result.Result);

            case GameRoundResultStatus.RoundNotFound:
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Spelrundan hittades inte.");

            case GameRoundResultStatus.RoundNotCompleted:
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Rundan är inte avslutad.",
                    detail:
                        "Resultatet visas först när båda deltagarna " +
                        "har skickat in svar och gissning.");

            default:
                throw new InvalidOperationException(
                    "Okänd status för spelresultat.");
        }
    }

    [HttpGet("matches/{matchId:guid}/progress")]
    public async Task<ActionResult<GameProgressResponseDto>> GetProgress(
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var progress = await _service.GetProgressAsync(
            matchId,
            userId,
            cancellationToken);

        if (progress is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Matchningen hittades inte.");
        }

        return Ok(progress);
    }
}
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Duett.API.Controllers;

[ApiController]
[Authorize]
[Route("api/matches/{matchId:guid}/questions")]
public class MatchQuestionsController : ControllerBase
{
    private readonly IMatchQuestionService _service;

    public MatchQuestionsController(IMatchQuestionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<MatchQuestionResponseDto>>> GetQuestions(
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _service.GetQuestionsAsync(
            matchId,
            userId,
            cancellationToken);

        if (result.Status != MatchQuestionStatus.Success)
        {
            return StatusToProblem(result.Status);
        }

        return Ok(result.Questions);
    }

    [HttpPost]
    public async Task<ActionResult<MatchQuestionResponseDto>> Create(
        Guid matchId,
        [FromBody] CreateMatchQuestionDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _service.CreateAsync(
                matchId,
                userId,
                dto,
                cancellationToken);

            if (result.Status != MatchQuestionStatus.Success)
            {
                return StatusToProblem(result.Status);
            }

            return StatusCode(
                StatusCodes.Status201Created,
                result.Question);
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ogiltig fråga.",
                detail: ex.Message);
        }
    }

    [HttpPost("{questionId:guid}/answer")]
    public async Task<ActionResult<MatchQuestionResponseDto>> Answer(
        Guid matchId,
        Guid questionId,
        [FromBody] AnswerMatchQuestionDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _service.AnswerAsync(
                matchId,
                questionId,
                userId,
                dto,
                cancellationToken);

            if (result.Status != MatchQuestionStatus.Success)
            {
                return StatusToProblem(result.Status);
            }

            return Ok(result.Question);
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ogiltigt svar.",
                detail: ex.Message);
        }
    }

    [HttpGet("progress")]
    public async Task<ActionResult<MatchQuestionProgressResponseDto>> GetProgress(
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

    private ObjectResult StatusToProblem(MatchQuestionStatus status)
    {
        return status switch
        {
            MatchQuestionStatus.MatchNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Matchningen hittades inte."),

            MatchQuestionStatus.QuestionsLocked => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Frågedelen är låst.",
                detail:
                    "Båda deltagarna måste ha minst tre rätta gissningar."),

            MatchQuestionStatus.QuestionLimitReached => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Du har redan ställt tre frågor.",
                detail:
                    "Varje deltagare får ställa högst tre egna frågor " +
                    "i matchningen."),

            MatchQuestionStatus.QuestionNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Frågan hittades inte."),

            MatchQuestionStatus.CannotAnswerOwnQuestion => Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Du kan inte besvara din egen fråga."),

            MatchQuestionStatus.AlreadyAnswered => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Frågan är redan besvarad.",
                detail: "Ett inskickat svar kan inte ändras."),

            _ => throw new InvalidOperationException(
                "Okänd status för matchningsfrågor.")
        };
    }
}
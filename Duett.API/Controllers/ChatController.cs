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
[Route("api/matches/{matchId:guid}/messages")]
public class ChatController : ControllerBase
{
    private readonly IChatService _service;

    public ChatController(IChatService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChatMessageResponseDto>>> GetMessages(
        Guid matchId,
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _service.GetMessagesAsync(
                matchId,
                userId,
                pageNumber,
                pageSize,
                cancellationToken);

            switch (result.Status)
            {
                case ChatAccessStatus.Success:
                    return Ok(result.Messages);

                case ChatAccessStatus.MatchNotFound:
                    return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Matchningen hittades inte.");

                case ChatAccessStatus.ChatLocked:
                    return Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Chatten är låst.",
                        detail:
                            "Båda deltagarna måste ha minst " +
                            "tre rätta gissningar för att öppna chatten.");

                default:
                    throw new InvalidOperationException(
                        "Okänd status för chattåtkomst.");
            }
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ogiltig förfrågan.",
                detail: ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<ChatMessageResponseDto>> Send(
        Guid matchId,
        [FromBody] SendChatMessageDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _service.SendAsync(
                matchId,
                userId,
                dto,
                cancellationToken);

            switch (result.Status)
            {
                case ChatAccessStatus.Success:
                    return StatusCode(
                        StatusCodes.Status201Created,
                        result.Message);

                case ChatAccessStatus.MatchNotFound:
                    return Problem(
                        statusCode: StatusCodes.Status404NotFound,
                        title: "Matchningen hittades inte.");

                case ChatAccessStatus.ChatLocked:
                    return Problem(
                        statusCode: StatusCodes.Status403Forbidden,
                        title: "Chatten är låst.",
                        detail:
                            "Båda deltagarna måste ha minst " +
                            "tre rätta gissningar för att öppna chatten.");

                default:
                    throw new InvalidOperationException(
                        "Okänd status för skickat chattmeddelande.");
            }
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ogiltigt meddelande.",
                detail: ex.Message);
        }
    }
}
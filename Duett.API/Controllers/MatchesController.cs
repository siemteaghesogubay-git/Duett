using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duett.API.Dtos;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Duett.API.Controllers;

[ApiController]
[Authorize]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
    private readonly IMatchService _service;
    private readonly IUserMatchService _userMatchService;

    public MatchesController(
        IMatchService service,
        IUserMatchService userMatchService)
    {
        _service = service;
        _userMatchService = userMatchService;
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<List<MatchSuggestionDto>>> GetSuggestions(
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
            var suggestions = await _service.GetSuggestionsAsync(
                userId,
                pageNumber,
                pageSize,
                cancellationToken);

            return Ok(suggestions);
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ogiltig sidindelning.",
                detail: ex.Message);
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<List<UserMatchResponseDto>>> GetMyMatches(
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
            var matches = await _userMatchService.GetMyMatchesAsync(
                userId,
                pageNumber,
                pageSize,
                cancellationToken);

            return Ok(matches);
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ogiltig sidindelning.",
                detail: ex.Message);
        }
    }
}
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duett.API.Dtos;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Duett.API.Controllers;

[ApiController]
[Authorize]
[Route("api/user-safety")]
public class UserSafetyController : ControllerBase
{
    private readonly IUserSafetyService _service;

    public UserSafetyController(IUserSafetyService service)
    {
        _service = service;
    }

    [HttpPost("blocks")]
    [EnableRateLimiting("UserSafety")]
    public async Task<IActionResult> Block(
        [FromBody] BlockUserDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            await _service.BlockAsync(
                userId,
                dto,
                cancellationToken);

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Blockeringen kunde inte genomföras.",
                detail: ex.Message);
        }
    }

    [HttpDelete("blocks/{blockedUserId}")]
    [EnableRateLimiting("UserSafety")]
    public async Task<IActionResult> Unblock(
        string blockedUserId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var wasRemoved = await _service.UnblockAsync(
                userId,
                blockedUserId,
                cancellationToken);

            if (!wasRemoved)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Blockeringen hittades inte.");
            }

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Blockeringen kunde inte tas bort.",
                detail: ex.Message);
        }
    }

    [HttpPost("reports")]
    [EnableRateLimiting("UserSafety")]
    public async Task<IActionResult> CreateReport(
        [FromBody] CreateUserReportDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var reportId = await _service.CreateReportAsync(
                userId,
                dto,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    reportId,
                    message = "The report has been received."
                });
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Rapporten kunde inte skickas.",
                detail: ex.Message);
        }
    }
}
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duett.API.Dtos;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Duett.API.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _service;

    public AdminController(IAdminService service)
    {
        _service = service;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserResponseDto>>> GetUsers(
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var users = await _service.GetUsersAsync(
                pageNumber,
                pageSize,
                cancellationToken);

            return Ok(users);
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ogiltig sidindelning.",
                detail: ex.Message);
        }
    }

    [HttpGet("reports")]
    public async Task<ActionResult<List<AdminReportResponseDto>>> GetReports(
        CancellationToken cancellationToken,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var reports = await _service.GetReportsAsync(
                status,
                pageNumber,
                pageSize,
                cancellationToken);

            return Ok(reports);
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ogiltig rapportfiltrering.",
                detail: ex.Message);
        }
    }

    [HttpPatch("reports/{reportId:guid}")]
    public async Task<IActionResult> ReviewReport(
        Guid reportId,
        [FromBody] ReviewUserReportDto dto,
        CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var wasReviewed = await _service.ReviewReportAsync(
                reportId,
                adminUserId,
                dto,
                cancellationToken);

            if (!wasReviewed)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Rapporten hittades inte.");
            }

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Rapporten kunde inte uppdateras.",
                detail: ex.Message);
        }
    }

    [HttpPatch("users/{userId}/deactivate")]
    public async Task<IActionResult> DeactivateUser(
        string userId,
        CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var wasDeactivated = await _service.DeactivateUserAsync(
                userId,
                adminUserId,
                cancellationToken);

            if (!wasDeactivated)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Användaren hittades inte.");
            }

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Kontot kunde inte avaktiveras.",
                detail: ex.Message);
        }
    }

    [HttpPatch("users/{userId}/reactivate")]
    public async Task<IActionResult> ReactivateUser(
        string userId,
        CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var wasReactivated = await _service.ReactivateUserAsync(
                userId,
                adminUserId,
                cancellationToken);

            if (!wasReactivated)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Användaren hittades inte.");
            }

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Kontot kunde inte återaktiveras.",
                detail: ex.Message);
        }
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(
        string userId,
        CancellationToken cancellationToken)
    {
        var adminUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return Unauthorized();
        }

        try
        {
            var wasDeleted = await _service.DeleteUserAsync(
                userId,
                adminUserId,
                cancellationToken);

            if (!wasDeleted)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Användaren hittades inte.");
            }

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Kontot kunde inte raderas.",
                detail: ex.Message);
        }
    }
}
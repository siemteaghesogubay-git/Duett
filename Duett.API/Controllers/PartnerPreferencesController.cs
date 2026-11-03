using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duett.API.Dtos;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Duett.API.Controllers;

[ApiController]
[Authorize]
[Route("api/partner-preferences")]
public class PartnerPreferencesController : ControllerBase
{
    private readonly IPartnerPreferencesService _service;

    public PartnerPreferencesController(
        IPartnerPreferencesService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<ActionResult<PartnerPreferencesResponseDto>> GetMine(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var preferences = await _service.GetMyPreferencesAsync(
            userId,
            cancellationToken);

        if (preferences is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Partnerönskemål saknas.",
                detail: "Du har inte sparat några partnerönskemål ännu.");
        }

        return Ok(preferences);
    }

    [HttpPut("me")]
    public async Task<IActionResult> Save(
        [FromBody] SavePartnerPreferencesDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            await _service.SaveAsync(
                userId,
                dto,
                cancellationToken);

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ogiltiga partnerönskemål.",
                detail: ex.Message);
        }
    }
}
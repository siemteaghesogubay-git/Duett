using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duett.API.Dtos;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Duett.API.Controllers;

[ApiController]
[Authorize]
[Route("api/profile-likes")]
public class ProfileLikesController : ControllerBase
{
    private readonly IProfileLikeService _service;

    public ProfileLikesController(IProfileLikeService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<LikeProfileResponseDto>> Like(
        [FromBody] LikeProfileDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _service.LikeAsync(
                userId,
                dto,
                cancellationToken);

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Gillningen kunde inte genomföras.",
                detail: ex.Message);
        }
    }
}
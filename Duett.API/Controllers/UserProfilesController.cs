using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duett.API.Dtos;
using Duett.API.Exceptions;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Duett.API.Controllers;

[ApiController]
[Authorize]
[Route("api/profiles")]
public class UserProfilesController : ControllerBase
{
    private readonly IUserProfileService _service;

    public UserProfilesController(IUserProfileService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponseDto>> GetMyProfile(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var profile = await _service.GetMyProfileAsync(
            userId,
            cancellationToken);

        if (profile is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Profil saknas.",
                detail: "Du har inte skapat någon profil ännu.");
        }

        return Ok(profile);
    }

    [HttpPost]
    public async Task<ActionResult<UserProfileResponseDto>> Create(
        [FromBody] CreateUserProfileDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var profile = await _service.CreateAsync(
                userId,
                dto,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetMyProfile),
                profile);
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ogiltiga profiluppgifter.",
                detail: ex.Message);
        }
        catch (ProfileAlreadyExistsException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Profilen finns redan.",
                detail: ex.Message);
        }
    }


    [HttpPut("me")]
    public async Task<IActionResult> Update(
    [FromBody] UpdateUserProfileDto dto,
    CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var updated = await _service.UpdateAsync(
                userId,
                dto,
                cancellationToken);

            if (!updated)
            {
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Profil saknas.",
                    detail: "Du har inte skapat någon profil ännu.");
            }

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ogiltiga profiluppgifter.",
                detail: ex.Message);
        }
    }
}
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
[Route("api/profile-photos")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public class ProfilePhotosController : ControllerBase
{
    private readonly IProfilePhotoService _service;

    public ProfilePhotosController(IProfilePhotoService service)
    {
        _service = service;
    }

    [HttpPut("me")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<ProfilePhotoResponseDto>> Upload(
        [FromForm] UploadProfilePhotoDto dto,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _service.UploadAsync(
                userId,
                dto,
                cancellationToken);

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Ogiltig profilbild.",
                detail: ex.Message);
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyPhoto(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _service.GetMyPhotoAsync(
            userId,
            cancellationToken);

        return CreatePhotoResponse(result.Status, result.File);
    }

    [HttpGet("matches/{matchId:guid}/partner")]
    public async Task<IActionResult> GetPartnerPhoto(
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _service.GetPartnerPhotoAsync(
            matchId,
            userId,
            cancellationToken);

        return CreatePhotoResponse(result.Status, result.File);
    }

    private IActionResult CreatePhotoResponse(
        ProfilePhotoStatus status,
        ProfilePhotoFile? photo)
    {
        switch (status)
        {
            case ProfilePhotoStatus.Success:
                if (photo is null)
                {
                    throw new InvalidOperationException(
                        "En lyckad bildhämtning saknar bildinnehåll.");
                }

                Response.Headers["Cache-Control"] = "no-store";
                Response.Headers["X-Content-Type-Options"] = "nosniff";

                // ASP.NET Core stänger strömmen när svaret hanterats.
                return File(photo.Content, photo.ContentType);

            case ProfilePhotoStatus.PhotoNotFound:
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Profilbilden hittades inte.");

            case ProfilePhotoStatus.MatchNotFound:
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Matchningen hittades inte.");

            case ProfilePhotoStatus.PhotosLocked:
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Profilbilderna är låsta.",
                    detail:
                        "Båda måste ha nått nivå 3 och besvarat " +
                        "varandras tre egna frågor.");

            default:
                throw new InvalidOperationException(
                    "Okänd status för bildhämtning.");
        }
    }
}
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

    public ProfilePhotosController(
        IProfilePhotoService service)
    {
        _service = service;
    }

    [HttpPost("me")]
    [HttpPut("me")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<ProfilePhotoResponseDto>> Upload(
        [FromForm] UploadProfilePhotoDto dto,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
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
                title: "Invalid profile photo.",
                detail: ex.Message);
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<List<ProfilePhotoResponseDto>>>
        GetMyPhotos(
            CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var photos = await _service.GetMyPhotosAsync(
            userId,
            cancellationToken);

        return Ok(photos);
    }

    [HttpGet("me/{photoId:guid}")]
    public async Task<IActionResult> GetMyPhotoFile(
        Guid photoId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _service.GetMyPhotoAsync(
            userId,
            photoId,
            cancellationToken);

        return CreatePhotoResponse(
            result.Status,
            result.File);
    }

    [HttpDelete("me/{photoId:guid}")]
    public async Task<IActionResult> DeleteMyPhoto(
        Guid photoId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var deleted = await _service.DeleteAsync(
            userId,
            photoId,
            cancellationToken);

        if (!deleted)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Profile photo not found.");
        }

        return NoContent();
    }

    [HttpGet("matches/{matchId:guid}/partner")]
    public async Task<IActionResult> GetPartnerPhoto(
        Guid matchId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _service.GetPartnerPhotoAsync(
            matchId,
            userId,
            cancellationToken);

        return CreatePhotoResponse(
            result.Status,
            result.File);
    }

    [HttpGet("matches/{matchId:guid}/partner/{photoId:guid}")]
    public async Task<IActionResult> GetPartnerPhotoFile(
        Guid matchId,
        Guid photoId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _service.GetPartnerPhotoAsync(
            matchId,
            userId,
            photoId,
            cancellationToken);

        return CreatePhotoResponse(
            result.Status,
            result.File);
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(
            ClaimTypes.NameIdentifier);
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
                        "Photo content is missing.");
                }

                Response.Headers["Cache-Control"] = "no-store";
                Response.Headers["X-Content-Type-Options"] = "nosniff";

                return File(
                    photo.Content,
                    photo.ContentType);

            case ProfilePhotoStatus.PhotoNotFound:
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Profile photo not found.");

            case ProfilePhotoStatus.MatchNotFound:
                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Match not found.");

            case ProfilePhotoStatus.PhotosLocked:
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Profile photos are locked.",
                    detail:
                        "Both players must reach level 3 and " +
                        "answer each other's three questions.");

            default:
                throw new InvalidOperationException(
                    "Unknown profile photo status.");
        }
    }
    [HttpPatch("me/{photoId:guid}/primary")]
    public async Task<IActionResult> SetPrimaryPhoto(
    Guid photoId,
    CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var updated = await _service.SetPrimaryAsync(
            userId,
            photoId,
            cancellationToken);

        if (!updated)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Profile photo not found.");
        }

        return NoContent();
    }






}
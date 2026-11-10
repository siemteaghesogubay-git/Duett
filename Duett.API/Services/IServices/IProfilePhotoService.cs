using Duett.API.Dtos;
using Duett.API.Models;

namespace Duett.API.Services.IServices;

public interface IProfilePhotoService
{
    Task<ProfilePhotoResponseDto> UploadAsync(
        string userId,
        UploadProfilePhotoDto dto,
        CancellationToken cancellationToken = default);

    Task<(
        ProfilePhotoStatus Status,
        ProfilePhotoFile? File)> GetMyPhotoAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<(
        ProfilePhotoStatus Status,
        ProfilePhotoFile? File)> GetPartnerPhotoAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken = default);
}
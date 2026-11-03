using Duett.API.Dtos;

namespace Duett.API.Services.IServices;

public interface IProfileLikeService
{
    Task<LikeProfileResponseDto> LikeAsync(
        string fromUserId,
        LikeProfileDto dto,
        CancellationToken cancellationToken = default);
}
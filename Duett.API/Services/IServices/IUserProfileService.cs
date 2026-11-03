using Duett.API.Dtos;

namespace Duett.API.Services.IServices;

public interface IUserProfileService
{
    Task<UserProfileResponseDto?> GetMyProfileAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<UserProfileResponseDto> CreateAsync(
        string userId,
        CreateUserProfileDto dto,
        CancellationToken cancellationToken = default);




    Task<bool> UpdateAsync(
    string userId,
    UpdateUserProfileDto dto,
    CancellationToken cancellationToken = default);
}

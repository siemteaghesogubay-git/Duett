using Duett.API.Dtos;

namespace Duett.API.Services.IServices;

public interface IPartnerPreferencesService
{
    Task<PartnerPreferencesResponseDto?> GetMyPreferencesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        string userId,
        SavePartnerPreferencesDto dto,
        CancellationToken cancellationToken = default);
}
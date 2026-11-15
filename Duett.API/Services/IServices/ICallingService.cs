using Duett.API.Dtos;

namespace Duett.API.Services.IServices;

public interface ICallingService
{
    Task<CallingTokenResponseDto?> GetCallingTokenAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken = default);
}
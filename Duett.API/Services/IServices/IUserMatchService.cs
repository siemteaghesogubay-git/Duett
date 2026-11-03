using Duett.API.Dtos;

namespace Duett.API.Services.IServices;

public interface IUserMatchService
{
    Task<List<UserMatchResponseDto>> GetMyMatchesAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
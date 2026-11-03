using Duett.API.Models;

namespace Duett.API.Repositories.IRepositories;

public interface IMatchRepository
{
    Task<List<UserProfile>> GetSuggestionsAsync(
        string userId,
        DateOnly today,
        int skip,
        int take,
        CancellationToken cancellationToken = default);


    Task<bool> CanMatchAsync(
        string userId,
        string otherUserId,
        DateOnly today,
        CancellationToken cancellationToken = default);
}
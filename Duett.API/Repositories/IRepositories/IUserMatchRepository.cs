using Duett.API.Models;

namespace Duett.API.Repositories.IRepositories;

public interface IUserMatchRepository
{
    Task<UserMatch> GetOrCreateAsync(
        string firstUserId,
        string secondUserId,
        CancellationToken cancellationToken = default);



    Task<List<UserMatchDetails>> GetForUserAsync(
    string userId,
    int skip,
    int take,
    CancellationToken cancellationToken = default);
}

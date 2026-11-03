using Duett.API.Models;

namespace Duett.API.Repositories.IRepositories;

public interface IProfileLikeRepository
{
    Task<bool> ExistsAsync(
        string fromUserId,
        string toUserId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ProfileLike like,
        CancellationToken cancellationToken = default);


}
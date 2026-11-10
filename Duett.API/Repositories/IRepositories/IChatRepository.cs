using Duett.API.Models;

namespace Duett.API.Repositories.IRepositories;

public interface IChatRepository
{
    Task<List<ChatMessage>> GetMessagesAsync(
        Guid matchId,
        string userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<ChatMessage?> AddAsync(
        Guid matchId,
        string userId,
        string text,
        CancellationToken cancellationToken = default);
}
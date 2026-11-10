using Duett.API.Dtos;
using Duett.API.Models;

namespace Duett.API.Services.IServices;

public interface IChatService
{
    Task<(
        ChatAccessStatus Status,
        List<ChatMessageResponseDto>? Messages)> GetMessagesAsync(
        Guid matchId,
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(
        ChatAccessStatus Status,
        ChatMessageResponseDto? Message)> SendAsync(
        Guid matchId,
        string userId,
        SendChatMessageDto dto,
        CancellationToken cancellationToken = default);


}
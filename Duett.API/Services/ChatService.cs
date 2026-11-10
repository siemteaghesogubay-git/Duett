using System.ComponentModel.DataAnnotations;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;

namespace Duett.API.Services;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepository;
    private readonly IGameRepository _gameRepository;

    public ChatService(
        IChatRepository chatRepository,
        IGameRepository gameRepository)
    {
        _chatRepository = chatRepository;
        _gameRepository = gameRepository;
    }

    public async Task<(
        ChatAccessStatus Status,
        List<ChatMessageResponseDto>? Messages)> GetMessagesAsync(
        Guid matchId,
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            throw new ValidationException(
                "Sidnumret måste vara minst 1.");
        }

        if (pageSize < 1 || pageSize > 50)
        {
            throw new ValidationException(
                "Antalet meddelanden per sida måste vara mellan 1 och 50.");
        }

        var skip = ((long)pageNumber - 1) * pageSize;

        if (skip > int.MaxValue)
        {
            throw new ValidationException(
                "Sidnumret är för stort.");
        }

        var access = await CheckAccessAsync(
            matchId,
            userId,
            cancellationToken);

        if (access != ChatAccessStatus.Success)
        {
            return (access, null);
        }

        var messages = await _chatRepository.GetMessagesAsync(
            matchId,
            userId,
            (int)skip,
            pageSize,
            cancellationToken);

        return (
            ChatAccessStatus.Success,
            messages.Select(MapToDto).ToList());
    }

    public async Task<(
        ChatAccessStatus Status,
        ChatMessageResponseDto? Message)> SendAsync(
        Guid matchId,
        string userId,
        SendChatMessageDto dto,
        CancellationToken cancellationToken = default)
    {
        var text = dto.Text?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            throw new ValidationException(
                "Meddelandet får inte vara tomt.");
        }

        if (text.Length > 2000)
        {
            throw new ValidationException(
                "Meddelandet får innehålla högst 2000 tecken.");
        }

        var access = await CheckAccessAsync(
            matchId,
            userId,
            cancellationToken);

        if (access != ChatAccessStatus.Success)
        {
            return (access, null);
        }

        var message = await _chatRepository.AddAsync(
            matchId,
            userId,
            text,
            cancellationToken);

        if (message is null)
        {
            return (ChatAccessStatus.MatchNotFound, null);
        }

        return (ChatAccessStatus.Success, MapToDto(message));
    }

    private async Task<ChatAccessStatus> CheckAccessAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Repositoryt returnerar null även om användaren
        // inte är deltagare i matchningen.
        var progress = await _gameRepository.GetProgressForUserAsync(
            matchId,
            userId,
            cancellationToken);

        if (progress is null)
        {
            return ChatAccessStatus.MatchNotFound;
        }

        if (progress.MyCorrectGuessCount < 3 ||
            progress.PartnerCorrectGuessCount < 3)
        {
            return ChatAccessStatus.ChatLocked;
        }

        return ChatAccessStatus.Success;
    }

    private static ChatMessageResponseDto MapToDto(
        ChatMessage message)
    {
        // Databasfältet innehåller UTC, men datetime2
        // bevarar inte DateTime.Kind.
        var createdAtUtc = DateTime.SpecifyKind(
            message.CreatedAtUtc,
            DateTimeKind.Utc);

        return new ChatMessageResponseDto
        {
            Id = message.Id,
            MatchId = message.MatchId,
            SenderUserId = message.SenderUserId,
            Text = message.Text,
            CreatedAtUtc = new DateTimeOffset(createdAtUtc)
        };
    }
}
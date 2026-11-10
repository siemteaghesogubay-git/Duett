using Duett.API.Data;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly ApplicationDbContext _context;

    public ChatRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(
        Guid matchId,
        string userId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(skip));
        }

        if (take < 1 || take > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(take));
        }

        // Bara deltagarna får hämta matchningens meddelanden.
        return await _context.ChatMessages
            .AsNoTracking()
            .Where(message =>
                message.MatchId == matchId
                && (message.Match.FirstUserId == userId
                    || message.Match.SecondUserId == userId))
            .OrderByDescending(message => message.CreatedAtUtc)
            .ThenByDescending(message => message.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatMessage?> AddAsync(
        Guid matchId,
        string userId,
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Kontrollera deltagaren innan meddelandet sparas.
        var isParticipant = await _context.UserMatches
            .AnyAsync(
                match => match.Id == matchId
                    && (match.FirstUserId == userId
                        || match.SecondUserId == userId),
                cancellationToken);

        if (!isParticipant)
        {
            return null;
        }

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            SenderUserId = userId,
            Text = text,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ChatMessages.Add(message);

        await _context.SaveChangesAsync(cancellationToken);

        return message;
    }

}
using System.ComponentModel.DataAnnotations;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;

namespace Duett.API.Services;

public class ProfileLikeService : IProfileLikeService
{
    private readonly IProfileLikeRepository _likeRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IUserMatchRepository _userMatchRepository;

    public ProfileLikeService(
        IProfileLikeRepository likeRepository,
        IMatchRepository matchRepository,
        IUserMatchRepository userMatchRepository)
    {
        _likeRepository = likeRepository;
        _matchRepository = matchRepository;
        _userMatchRepository = userMatchRepository;
    }

    public async Task<LikeProfileResponseDto> LikeAsync(
        string fromUserId,
        LikeProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var toUserId = dto.ToUserId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(toUserId) ||
            toUserId.Length > 450)
        {
            throw new ValidationException(
                "Ange ett giltigt användar-ID.");
        }

        if (string.Equals(
            fromUserId,
            toUserId,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                "Du kan inte gilla din egen profil.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var canMatch = await _matchRepository.CanMatchAsync(
            fromUserId,
            toUserId,
            today,
            cancellationToken);

        if (!canMatch)
        {
            throw new ValidationException(
                "Profilen är inte tillgänglig för en gillning. " +
                "Båda måste ha profiler och partnerönskemål " +
                "som passar varandra.");
        }

        var alreadyLiked = await _likeRepository.ExistsAsync(
            fromUserId,
            toUserId,
            cancellationToken);

        if (!alreadyLiked)
        {
            var like = new ProfileLike
            {
                FromUserId = fromUserId,
                ToUserId = toUserId,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _likeRepository.AddAsync(
                like,
                cancellationToken);
        }

        // Kontrollera om mottagaren har gillat tillbaka.
        var isMutualLike = await _likeRepository.ExistsAsync(
            toUserId,
            fromUserId,
            cancellationToken);

        Guid? matchId = null;

        if (isMutualLike)
        {
            // Skapa matchningen eller hämta den befintliga.
            var match = await _userMatchRepository.GetOrCreateAsync(
                fromUserId,
                toUserId,
                cancellationToken);

            matchId = match.Id;
        }

        return new LikeProfileResponseDto
        {
            ToUserId = toUserId,
            IsMutualLike = isMutualLike,
            MatchId = matchId
        };
    }
}
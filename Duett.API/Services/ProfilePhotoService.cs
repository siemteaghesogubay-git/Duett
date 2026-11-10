using System.ComponentModel.DataAnnotations;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;
using Duett.API.Storage.IStorage;

namespace Duett.API.Services;

public class ProfilePhotoService : IProfilePhotoService
{
    private readonly IProfilePhotoRepository _repository;
    private readonly IProfilePhotoProcessor _processor;
    private readonly IPrivatePhotoStorage _storage;
    private readonly IGameRepository _gameRepository;
    private readonly IMatchQuestionService _questionService;
    private readonly ILogger<ProfilePhotoService> _logger;

    public ProfilePhotoService(
        IProfilePhotoRepository repository,
        IProfilePhotoProcessor processor,
        IPrivatePhotoStorage storage,
        IGameRepository gameRepository,
        IMatchQuestionService questionService,
        ILogger<ProfilePhotoService> logger)
    {
        _repository = repository;
        _processor = processor;
        _storage = storage;
        _gameRepository = gameRepository;
        _questionService = questionService;
        _logger = logger;
    }

    public async Task<ProfilePhotoResponseDto> UploadAsync(
        string userId,
        UploadProfilePhotoDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (dto.File is null)
        {
            throw new ValidationException(
                "Välj en profilbild.");
        }

        // Validera och skapa en ny JPEG utan originalets metadata.
        var processed = await _processor.ProcessAsync(
            dto.File,
            cancellationToken);

        using var content = new MemoryStream(
            processed.Content,
            writable: false);

        // Filen måste finnas innan databasen börjar peka på den.
        var storageKey = await _storage.SaveAsync(
            content,
            cancellationToken);

        (string? PreviousStorageKey, DateTime UploadedAtUtc) saved;

        try
        {
            saved = await _repository.ReplaceAsync(
                userId,
                storageKey,
                processed.ContentType,
                processed.SizeBytes,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Vid ett anslutningsfel kan commit ha lyckats
            // trots att anropet rapporterar ett fel.
            // Radera därför inte den nya filen här.
            _logger.LogError(
                ex,
                "Databasuppdateringen för profilbilden kunde inte " +
                "bekräftas. Fil {StorageKey} behålls för avstämning.",
                storageKey);

            throw;
        }

        // Databasen pekar nu på den nya bilden.
        if (saved.PreviousStorageKey is string previousKey &&
            previousKey != storageKey)
        {
            await TryDeleteOldPhotoAsync(previousKey);
        }

        return new ProfilePhotoResponseDto
        {
            ContentType = processed.ContentType,
            SizeBytes = processed.SizeBytes,
            UploadedAtUtc = new DateTimeOffset(
                DateTime.SpecifyKind(
                    saved.UploadedAtUtc,
                    DateTimeKind.Utc))
        };
    }

    public async Task<(
        ProfilePhotoStatus Status,
        ProfilePhotoFile? File)> GetMyPhotoAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await OpenPhotoAsync(
            userId,
            cancellationToken);
    }

    public async Task<(
        ProfilePhotoStatus Status,
        ProfilePhotoFile? File)> GetPartnerPhotoAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Kontrollerar att den inloggade användaren är deltagare.
        var match = await _gameRepository.GetMatchForUserAsync(
            matchId,
            userId,
            cancellationToken);

        if (match is null)
        {
            return (ProfilePhotoStatus.MatchNotFound, null);
        }

        // Kräver nivå 3 för båda och tre besvarade frågor var.
        var progress = await _questionService.GetProgressAsync(
            matchId,
            userId,
            cancellationToken);

        if (progress is null)
        {
            return (ProfilePhotoStatus.MatchNotFound, null);
        }

        if (!progress.ArePhotosUnlocked)
        {
            return (ProfilePhotoStatus.PhotosLocked, null);
        }

        // Partnerns ID bestäms av backend.
        var partnerUserId = string.Equals(
            match.FirstUserId,
            userId,
            StringComparison.OrdinalIgnoreCase)
                ? match.SecondUserId
                : match.FirstUserId;

        return await OpenPhotoAsync(
            partnerUserId,
            cancellationToken);
    }

    private async Task<(
        ProfilePhotoStatus Status,
        ProfilePhotoFile? File)> OpenPhotoAsync(
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        var photo = await _repository.GetByUserIdAsync(
            ownerUserId,
            cancellationToken);

        if (photo is null)
        {
            return (ProfilePhotoStatus.PhotoNotFound, null);
        }

        var stream = await _storage.OpenReadAsync(
            photo.StorageKey,
            cancellationToken);

        if (stream is null)
        {
            return (ProfilePhotoStatus.PhotoNotFound, null);
        }

        // Strömmen lämnas öppen för controllerns filrespons.
        return (
            ProfilePhotoStatus.Success,
            new ProfilePhotoFile
            {
                Content = stream,
                ContentType = photo.ContentType
            });
    }

    private async Task TryDeleteOldPhotoAsync(string storageKey)
    {
        try
        {
            // Uppladdningen är redan sparad.
            // Städningen ska inte avbrytas av klientens frånkoppling.
            await _storage.DeleteAsync(
                storageKey,
                CancellationToken.None);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(
                ex,
                "Den gamla profilbilden {StorageKey} kunde inte tas bort.",
                storageKey);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                ex,
                "Åtkomst saknas för att ta bort profilbilden {StorageKey}.",
                storageKey);
        }
    }
}
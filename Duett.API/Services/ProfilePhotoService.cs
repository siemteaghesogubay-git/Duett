using System.ComponentModel.DataAnnotations;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;
using Duett.API.Storage.IStorage;

namespace Duett.API.Services;

public class ProfilePhotoService : IProfilePhotoService
{
    private const int MaxPhotos = 6;

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
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.File is null)
        {
            throw new ValidationException(
                "Choose a profile photo.");
        }

        var processed = await _processor.ProcessAsync(
            dto.File,
            cancellationToken);

        using var content = new MemoryStream(
            processed.Content,
            writable: false);

        var storageKey = await _storage.SaveAsync(
            content,
            cancellationToken);

        ProfilePhoto? savedPhoto;

        try
        {
            savedPhoto = await _repository.AddAsync(
                userId,
                storageKey,
                processed.ContentType,
                processed.SizeBytes,
                MaxPhotos,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Could not save profile photo {StorageKey}.",
                storageKey);

            throw;
        }

        if (savedPhoto is null)
        {
            await TryDeleteStorageAsync(storageKey);

            throw new ValidationException(
                $"You can upload a maximum of {MaxPhotos} profile photos.");
        }

        return MapToResponse(savedPhoto);
    }

    public async Task<List<ProfilePhotoResponseDto>> GetMyPhotosAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var photos = await _repository.GetByUserIdAsync(
            userId,
            cancellationToken);

        return photos
            .Select(MapToResponse)
            .ToList();
    }

    public Task<(
        ProfilePhotoStatus Status,
        ProfilePhotoFile? File)> GetMyPhotoAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return OpenPhotoAsync(
            userId,
            photoId: null,
            cancellationToken);
    }

    public Task<(
        ProfilePhotoStatus Status,
        ProfilePhotoFile? File)> GetMyPhotoAsync(
        string userId,
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        return OpenPhotoAsync(
            userId,
            photoId,
            cancellationToken);
    }

    public Task<(
        ProfilePhotoStatus Status,
        ProfilePhotoFile? File)> GetPartnerPhotoAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return GetPartnerPhotoCoreAsync(
            matchId,
            userId,
            photoId: null,
            cancellationToken);
    }

    public Task<(
        ProfilePhotoStatus Status,
        ProfilePhotoFile? File)> GetPartnerPhotoAsync(
        Guid matchId,
        string userId,
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        return GetPartnerPhotoCoreAsync(
            matchId,
            userId,
            photoId,
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        string userId,
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var deletedPhoto = await _repository.DeleteAsync(
            userId,
            photoId,
            cancellationToken);

        if (deletedPhoto is null)
        {
            return false;
        }

        await TryDeleteStorageAsync(
            deletedPhoto.StorageKey);

        return true;
    }

    private async Task<(
        ProfilePhotoStatus Status,
        ProfilePhotoFile? File)> GetPartnerPhotoCoreAsync(
        Guid matchId,
        string userId,
        Guid? photoId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var match = await _gameRepository.GetMatchForUserAsync(
            matchId,
            userId,
            cancellationToken);

        if (match is null)
        {
            return (ProfilePhotoStatus.MatchNotFound, null);
        }

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

        var partnerUserId = string.Equals(
            match.FirstUserId,
            userId,
            StringComparison.OrdinalIgnoreCase)
                ? match.SecondUserId
                : match.FirstUserId;

        return await OpenPhotoAsync(
            partnerUserId,
            photoId,
            cancellationToken);
    }

    private async Task<(
        ProfilePhotoStatus Status,
        ProfilePhotoFile? File)> OpenPhotoAsync(
        string ownerUserId,
        Guid? photoId,
        CancellationToken cancellationToken)
    {
        var photos = await _repository.GetByUserIdAsync(
            ownerUserId,
            cancellationToken);

        var photo = photoId.HasValue
            ? photos.SingleOrDefault(item => item.Id == photoId.Value)
            : photos.FirstOrDefault();

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

        return (
            ProfilePhotoStatus.Success,
            new ProfilePhotoFile
            {
                Content = stream,
                ContentType = photo.ContentType
            });
    }

    private async Task TryDeleteStorageAsync(
        string storageKey)
    {
        try
        {
            await _storage.DeleteAsync(
                storageKey,
                CancellationToken.None);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(
                ex,
                "Could not delete photo file {StorageKey}.",
                storageKey);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                ex,
                "Access denied when deleting photo file {StorageKey}.",
                storageKey);
        }
    }

    private static ProfilePhotoResponseDto MapToResponse(
        ProfilePhoto photo)
    {
        return new ProfilePhotoResponseDto
        {
            Id = photo.Id,
            SortOrder = photo.SortOrder,
            ContentType = photo.ContentType,
            SizeBytes = photo.SizeBytes,
            UploadedAtUtc = new DateTimeOffset(
                DateTime.SpecifyKind(
                    photo.UploadedAtUtc,
                    DateTimeKind.Utc))
        };
    }




        public async Task<bool> SetPrimaryAsync(
    string userId,
    Guid photoId,
    CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return await _repository.SetPrimaryAsync(
            userId,
            photoId,
            cancellationToken);
    }
}

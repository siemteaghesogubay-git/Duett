using System.ComponentModel.DataAnnotations;
using Duett.API.Dtos;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;

namespace Duett.API.Services;

public class MatchService : IMatchService
{
    private readonly IMatchRepository _repository;

    public MatchService(IMatchRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<MatchSuggestionDto>> GetSuggestionsAsync(
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
                "Antalet förslag per sida måste vara mellan 1 och 50.");
        }

        var skip = ((long)pageNumber - 1) * pageSize;

        if (skip > int.MaxValue)
        {
            throw new ValidationException(
                "Sidnumret är för stort.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var profiles = await _repository.GetSuggestionsAsync(
            userId,
            today,
            (int)skip,
            pageSize,
            cancellationToken);

        return profiles.Select(profile => new MatchSuggestionDto
        {
            UserId = profile.UserId,
            DisplayName = profile.DisplayName
        }).ToList();
    }
}
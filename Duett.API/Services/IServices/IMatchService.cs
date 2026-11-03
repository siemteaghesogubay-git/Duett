using Duett.API.Dtos;

namespace Duett.API.Services.IServices;

public interface IMatchService
{
    Task<List<MatchSuggestionDto>> GetSuggestionsAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
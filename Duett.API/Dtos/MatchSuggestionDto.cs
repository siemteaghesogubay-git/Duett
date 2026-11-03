namespace Duett.API.Dtos;

public class MatchSuggestionDto
{
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int Age { get; set; }

    public string City { get; set; } = string.Empty;
}
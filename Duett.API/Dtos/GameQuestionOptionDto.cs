namespace Duett.API.Dtos;

public class GameQuestionOptionDto
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
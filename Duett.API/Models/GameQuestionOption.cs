using System.ComponentModel.DataAnnotations;

namespace Duett.API.Models;

public class GameQuestionOption
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionId { get; set; }

    public GameQuestion Question { get; set; } = null!;

    [MaxLength(150)]
    public string Text { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
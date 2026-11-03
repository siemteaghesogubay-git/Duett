using System.ComponentModel.DataAnnotations;

namespace Duett.API.Models;

public class GameQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(300)]
    public string Text { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<GameQuestionOption> Options { get; set; }
        = new List<GameQuestionOption>();
}
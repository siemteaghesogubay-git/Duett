using System.ComponentModel.DataAnnotations;

namespace Duett.API.Models;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MatchId { get; set; }

    public UserMatch Match { get; set; } = null!;

    [MaxLength(450)]
    public string SenderUserId { get; set; } = string.Empty;

    public ApplicationUser SenderUser { get; set; } = null!;

    [MaxLength(2000)]
    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
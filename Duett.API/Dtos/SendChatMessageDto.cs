using System.ComponentModel.DataAnnotations;

namespace Duett.API.Dtos;

public class SendChatMessageDto
{
    [Required(ErrorMessage = "Meddelandet får inte vara tomt.")]
    [StringLength(2000,
        ErrorMessage = "Meddelandet får innehålla högst 2000 tecken.")]
    public string Text { get; set; } = string.Empty;
}

// reponse DTO 
public class ChatMessageResponseDto
{
    public Guid Id { get; set; }

    public Guid MatchId { get; set; }

    public string SenderUserId { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
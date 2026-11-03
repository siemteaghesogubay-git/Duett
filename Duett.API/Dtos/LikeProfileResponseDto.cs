namespace Duett.API.Dtos;

public class LikeProfileResponseDto
{
    public string ToUserId { get; set; } = string.Empty;

    public bool IsMutualLike { get; set; }


    public Guid? MatchId { get; set; }
}
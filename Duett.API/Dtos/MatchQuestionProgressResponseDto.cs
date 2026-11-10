namespace Duett.API.Dtos;

public class MatchQuestionProgressResponseDto
{
    public Guid MatchId { get; set; }

    public bool IsQuestionStageUnlocked { get; set; }

    // Antal frågor som jag har ställt.
    public int MyQuestionCount { get; set; }

    // Mina frågor som partnern har besvarat.
    public int MyAnsweredQuestionCount { get; set; }

    // Antal frågor som partnern har ställt.
    public int PartnerQuestionCount { get; set; }

    // Partnerns frågor som jag har besvarat.
    public int PartnerAnsweredQuestionCount { get; set; }

    public bool ArePhotosUnlocked =>
        IsQuestionStageUnlocked
        && MyQuestionCount == 3
        && PartnerQuestionCount == 3
        && MyAnsweredQuestionCount == 3
        && PartnerAnsweredQuestionCount == 3;
}
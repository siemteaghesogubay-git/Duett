namespace Duett.API.Models;

public enum MatchQuestionStatus
{
    Success,
    MatchNotFound,
    QuestionsLocked,
    QuestionLimitReached,
    QuestionNotFound,
    CannotAnswerOwnQuestion,
    AlreadyAnswered

}
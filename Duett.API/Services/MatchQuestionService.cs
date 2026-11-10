using System.ComponentModel.DataAnnotations;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;

namespace Duett.API.Services;

public class MatchQuestionService : IMatchQuestionService
{
    private readonly IMatchQuestionRepository _repository;
    private readonly IGameRepository _gameRepository;

    public MatchQuestionService(
        IMatchQuestionRepository repository,
        IGameRepository gameRepository)
    {
        _repository = repository;
        _gameRepository = gameRepository;
    }

    public async Task<(
        MatchQuestionStatus Status,
        List<MatchQuestionResponseDto>? Questions)> GetQuestionsAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var access = await CheckAccessAsync(
            matchId,
            userId,
            cancellationToken);

        if (access != MatchQuestionStatus.Success)
        {
            return (access, null);
        }

        var questions = await _repository.GetQuestionsAsync(
            matchId,
            userId,
            cancellationToken);

        return (
            MatchQuestionStatus.Success,
            questions.Select(MapToDto).ToList());
    }

    public async Task<(
        MatchQuestionStatus Status,
        MatchQuestionResponseDto? Question)> CreateAsync(
        Guid matchId,
        string userId,
        CreateMatchQuestionDto dto,
        CancellationToken cancellationToken = default)
    {
        var text = dto.Text?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            throw new ValidationException(
                "Frågan får inte vara tom.");
        }

        if (text.Length > 300)
        {
            throw new ValidationException(
                "Frågan får innehålla högst 300 tecken.");
        }

        var access = await CheckAccessAsync(
            matchId,
            userId,
            cancellationToken);

        if (access != MatchQuestionStatus.Success)
        {
            return (access, null);
        }

        var result = await _repository.CreateAsync(
            matchId,
            userId,
            text,
            cancellationToken);

        if (result.Status != MatchQuestionStatus.Success)
        {
            return (result.Status, null);
        }

        var question = result.Question
            ?? throw new InvalidOperationException(
                "En lyckad skapning saknar en fråga.");

        return (MatchQuestionStatus.Success, MapToDto(question));
    }

    public async Task<(
        MatchQuestionStatus Status,
        MatchQuestionResponseDto? Question)> AnswerAsync(
        Guid matchId,
        Guid questionId,
        string userId,
        AnswerMatchQuestionDto dto,
        CancellationToken cancellationToken = default)
    {
        var answerText = dto.AnswerText?.Trim() ?? string.Empty;

        if (answerText.Length == 0)
        {
            throw new ValidationException(
                "Svaret får inte vara tomt.");
        }

        if (answerText.Length > 2000)
        {
            throw new ValidationException(
                "Svaret får innehålla högst 2000 tecken.");
        }

        var access = await CheckAccessAsync(
            matchId,
            userId,
            cancellationToken);

        if (access != MatchQuestionStatus.Success)
        {
            return (access, null);
        }

        var result = await _repository.AnswerAsync(
            matchId,
            questionId,
            userId,
            answerText,
            cancellationToken);

        if (result.Status != MatchQuestionStatus.Success)
        {
            return (result.Status, null);
        }

        var question = result.Question
            ?? throw new InvalidOperationException(
                "En lyckad besvaring saknar en fråga.");

        return (MatchQuestionStatus.Success, MapToDto(question));
    }

    private async Task<MatchQuestionStatus> CheckAccessAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Null betyder att matchningen saknas
        // eller att användaren inte är deltagare.
        var progress = await _gameRepository.GetProgressForUserAsync(
            matchId,
            userId,
            cancellationToken);

        if (progress is null)
        {
            return MatchQuestionStatus.MatchNotFound;
        }

        if (progress.MyCorrectGuessCount < 3 ||
            progress.PartnerCorrectGuessCount < 3)
        {
            return MatchQuestionStatus.QuestionsLocked;
        }

        return MatchQuestionStatus.Success;
    }

    private static MatchQuestionResponseDto MapToDto(
        MatchQuestion question)
    {
        return new MatchQuestionResponseDto
        {
            Id = question.Id,
            MatchId = question.MatchId,
            AskedByUserId = question.AskedByUserId,
            QuestionNumber = question.QuestionNumber,
            Text = question.Text,
            AnswerText = question.AnswerText,
            AnsweredByUserId = question.AnsweredByUserId,

            CreatedAtUtc = ToUtcOffset(question.CreatedAtUtc),

            AnsweredAtUtc = question.AnsweredAtUtc is DateTime answeredAt
                ? ToUtcOffset(answeredAt)
                : null
        };
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        // Databasens datetime2 innehåller UTC
        // men bevarar inte DateTime.Kind.
        return new DateTimeOffset(
            DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
    public async Task<MatchQuestionProgressResponseDto?> GetProgressAsync(
    Guid matchId,
    string userId,
    CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Kontrollerar även att användaren deltar i matchningen.
        var gameProgress = await _gameRepository.GetProgressForUserAsync(
            matchId,
            userId,
            cancellationToken);

        if (gameProgress is null)
        {
            return null;
        }

        var match = await _gameRepository.GetMatchForUserAsync(
            matchId,
            userId,
            cancellationToken);

        if (match is null)
        {
            return null;
        }

        var partnerUserId = string.Equals(
            match.FirstUserId,
            userId,
            StringComparison.OrdinalIgnoreCase)
                ? match.SecondUserId
                : match.FirstUserId;

        var questions = await _repository.GetQuestionsAsync(
            matchId,
            userId,
            cancellationToken);

        var myQuestions = questions
            .Where(question => string.Equals(
                question.AskedByUserId,
                userId,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        var partnerQuestions = questions
            .Where(question => string.Equals(
                question.AskedByUserId,
                partnerUserId,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new MatchQuestionProgressResponseDto
        {
            MatchId = matchId,

            IsQuestionStageUnlocked =
                gameProgress.MyCorrectGuessCount >= 3
                && gameProgress.PartnerCorrectGuessCount >= 3,

            MyQuestionCount = myQuestions.Count,

            MyAnsweredQuestionCount = myQuestions.Count(question =>
                question.AnsweredAtUtc.HasValue
                && !string.IsNullOrWhiteSpace(question.AnswerText)
                && string.Equals(
                    question.AnsweredByUserId,
                    partnerUserId,
                    StringComparison.OrdinalIgnoreCase)),

            PartnerQuestionCount = partnerQuestions.Count,

            PartnerAnsweredQuestionCount = partnerQuestions.Count(question =>
                question.AnsweredAtUtc.HasValue
                && !string.IsNullOrWhiteSpace(question.AnswerText)
                && string.Equals(
                    question.AnsweredByUserId,
                    userId,
                    StringComparison.OrdinalIgnoreCase))
        };
    }
}
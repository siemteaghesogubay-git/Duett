using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;

namespace Duett.API.Services;

public class GameService : IGameService
{
    private readonly IGameRepository _repository;

    public GameService(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<(
        StartGameRoundStatus Status,
        GameRoundResponseDto? Round)> StartRoundAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetOrCreateCurrentRoundAsync(
            matchId,
            userId,
            cancellationToken);

        if (result.Status != StartGameRoundStatus.Success)
        {
            return (result.Status, null);
        }

        var round = result.Round
            ?? throw new InvalidOperationException(
                "En lyckad spelstart saknar en runda.");

        var response = await CreateResponseAsync(
            round,
            userId,
            cancellationToken);

        return (StartGameRoundStatus.Success, response);
    }

    public async Task<GameRoundResponseDto?> GetRoundAsync(
        Guid roundId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var round = await _repository.GetRoundForUserAsync(
            roundId,
            userId,
            cancellationToken);

        if (round is null)
        {
            return null;
        }

        return await CreateResponseAsync(
            round,
            userId,
            cancellationToken);
    }

    private async Task<GameRoundResponseDto> CreateResponseAsync(
        GameRound round,
        string userId,
        CancellationToken cancellationToken)
    {
        var submissions = await _repository.GetSubmissionsForUserAsync(
            round.Id,
            userId,
            cancellationToken);

        return new GameRoundResponseDto
        {
            RoundId = round.Id,
            MatchId = round.MatchId,
            RoundNumber = round.RoundNumber,
            QuestionText = round.Question.Text,

            Options = round.Question.Options
                .OrderBy(option => option.SortOrder)
                .Select(option => new GameQuestionOptionDto
                {
                    Id = option.Id,
                    Text = option.Text,
                    SortOrder = option.SortOrder
                })
                .ToList(),

            HasSubmitted = submissions.Any(submission =>
                string.Equals(
                    submission.UserId,
                    userId,
                    StringComparison.OrdinalIgnoreCase)),

            PartnerHasSubmitted = submissions.Any(submission =>
                !string.Equals(
                    submission.UserId,
                    userId,
                    StringComparison.OrdinalIgnoreCase)),

            IsCompleted = round.CompletedAtUtc is not null
        };
    }


        public async Task<SubmitGameRoundStatus> SubmitAsync(
    Guid roundId,
    string userId,
    SubmitGameRoundDto dto,
    CancellationToken cancellationToken = default)
    {
        if (dto.AnswerOptionId is not Guid answerOptionId ||
            dto.GuessOptionId is not Guid guessOptionId ||
            answerOptionId == Guid.Empty ||
            guessOptionId == Guid.Empty)
        {
            return SubmitGameRoundStatus.InvalidOption;
        }

        return await _repository.SubmitAsync(
            roundId,
            userId,
            answerOptionId,
            guessOptionId,
            cancellationToken);
    }


    public async Task<(
    GameRoundResultStatus Status,
    GameRoundResultDto? Result)> GetResultAsync(
    Guid roundId,
    string userId,
    CancellationToken cancellationToken = default)
    {
        // Repository kontrollerar att användaren är deltagare.
        var round = await _repository.GetRoundForUserAsync(
            roundId,
            userId,
            cancellationToken);

        if (round is null)
        {
            return (GameRoundResultStatus.RoundNotFound, null);
        }

        // Avslöja inga val innan rundan är avslutad.
        if (round.CompletedAtUtc is null)
        {
            return (GameRoundResultStatus.RoundNotCompleted, null);
        }

        var match = await _repository.GetMatchForUserAsync(
            round.MatchId,
            userId,
            cancellationToken);

        if (match is null)
        {
            return (GameRoundResultStatus.RoundNotFound, null);
        }

        var partnerUserId = string.Equals(
            match.FirstUserId,
            userId,
            StringComparison.OrdinalIgnoreCase)
                ? match.SecondUserId
                : match.FirstUserId;

        var submissions = await _repository.GetSubmissionsForUserAsync(
            roundId,
            userId,
            cancellationToken);

        var mySubmission = submissions.SingleOrDefault(submission =>
            string.Equals(
                submission.UserId,
                userId,
                StringComparison.OrdinalIgnoreCase));

        var partnerSubmission = submissions.SingleOrDefault(submission =>
            string.Equals(
                submission.UserId,
                partnerUserId,
                StringComparison.OrdinalIgnoreCase));

        if (mySubmission is null || partnerSubmission is null)
        {
            throw new InvalidOperationException(
                "En avslutad runda saknar deltagarnas bidrag.");
        }

        var optionTexts = round.Question.Options
            .ToDictionary(option => option.Id, option => option.Text);

        var result = new GameRoundResultDto
        {
            RoundId = round.Id,
            RoundNumber = round.RoundNumber,
            QuestionText = round.Question.Text,

            MyAnswer = optionTexts[mySubmission.AnswerOptionId],
            MyGuess = optionTexts[mySubmission.GuessOptionId],

            PartnerAnswer = optionTexts[partnerSubmission.AnswerOptionId],
            PartnerGuess = optionTexts[partnerSubmission.GuessOptionId],

            MyGuessWasCorrect =
                mySubmission.GuessOptionId ==
                partnerSubmission.AnswerOptionId,

            PartnerGuessWasCorrect =
                partnerSubmission.GuessOptionId ==
                mySubmission.AnswerOptionId
        };

        return (GameRoundResultStatus.Success, result);
    }
    public async Task<GameProgressResponseDto?> GetProgressAsync(
    Guid matchId,
    string userId,
    CancellationToken cancellationToken = default)
    {
        var progress = await _repository.GetProgressForUserAsync(
            matchId,
            userId,
            cancellationToken);

        if (progress is null)
        {
            return null;
        }

        return new GameProgressResponseDto
        {
            MatchId = progress.MatchId,

            // Nivån stannar på 3 även om personen får fler rätt.
            MyLevel = Math.Clamp(
                progress.MyCorrectGuessCount,
                0,
                3),

            PartnerLevel = Math.Clamp(
                progress.PartnerCorrectGuessCount,
                0,
                3)
        };
    }



}

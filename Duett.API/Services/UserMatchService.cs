using System.ComponentModel.DataAnnotations;
using Duett.API.Dtos;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;

namespace Duett.API.Services;

public class UserMatchService : IUserMatchService
{
    private readonly IUserMatchRepository _repository;

    public UserMatchService(IUserMatchRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<UserMatchResponseDto>> GetMyMatchesAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            throw new ValidationException(
                "Sidnumret måste vara minst 1.");
        }

        if (pageSize < 1 || pageSize > 50)
        {
            throw new ValidationException(
                "Antalet matchningar per sida måste vara mellan 1 och 50.");
        }

        var skip = ((long)pageNumber - 1) * pageSize;

        if (skip > int.MaxValue)
        {
            throw new ValidationException(
                "Sidnumret är för stort.");
        }

        var matches = await _repository.GetForUserAsync(
            userId,
            (int)skip,
            pageSize,
            cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return matches.Select(match => new UserMatchResponseDto
        {
            MatchId = match.MatchId,
            OtherUserId = match.OtherUserId,
            DisplayName = match.DisplayName,

            Age = match.MyCorrectGuessCount >= 1
                ? CalculateAge(match.DateOfBirth, today)
                : null,

            City = match.MyCorrectGuessCount >= 2
                ? match.City
                : null,

            RelationshipMode = match.MyCorrectGuessCount >= 3
                ? match.RelationshipMode
                : null,

            CreatedAtUtc = match.CreatedAtUtc
        }).ToList();
    }

    private static int CalculateAge(
        DateOnly dateOfBirth,
        DateOnly today)
    {
        var age = today.Year - dateOfBirth.Year;

        if (dateOfBirth.Month > today.Month ||
            (dateOfBirth.Month == today.Month &&
             dateOfBirth.Day > today.Day))
        {
            age--;
        }

        return age;
    }
}
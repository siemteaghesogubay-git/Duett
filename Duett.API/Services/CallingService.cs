using Azure.Communication;
using Azure.Communication.Identity;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Repositories.IRepositories;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Identity;

namespace Duett.API.Services;

public class CallingService : ICallingService
{
    private static readonly CommunicationTokenScope[] CallingScopes =
    [
        CommunicationTokenScope.VoIP
    ];

    private readonly CommunicationIdentityClient _identityClient;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<CallingService> _logger;

    public CallingService(
        CommunicationIdentityClient identityClient,
        UserManager<ApplicationUser> userManager,
        IGameRepository gameRepository,
        ILogger<CallingService> logger)
    {
        _identityClient = identityClient;
        _userManager = userManager;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    public async Task<CallingTokenResponseDto?> GetCallingTokenAsync(
        Guid matchId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // GetMatchForUserAsync returnerar null om användaren inte
        // tillhör matchningen eller om någon har blockerat den andra.
        var match = await _gameRepository.GetMatchForUserAsync(
            matchId,
            userId,
            cancellationToken);

        if (match is null)
        {
            return null;
        }

        var peerUserId = match.FirstUserId == userId
            ? match.SecondUserId
            : match.FirstUserId;

        var user = await _userManager.FindByIdAsync(userId);
        var peerUser = await _userManager.FindByIdAsync(peerUserId);

        if (user is null || peerUser is null)
        {
            return null;
        }

        var communicationUserId = await EnsureCommunicationUserAsync(
            user,
            cancellationToken);

        var peerCommunicationUserId =
            await EnsureCommunicationUserAsync(
                peerUser,
                cancellationToken);

        var communicationUser = new CommunicationUserIdentifier(
            communicationUserId);

        var tokenResponse = await _identityClient.GetTokenAsync(
            communicationUser,
            CallingScopes,
            tokenExpiresIn: TimeSpan.FromHours(1),
            cancellationToken);

        _logger.LogInformation(
            "VoIP-token utfärdad för användare {UserId} i matchning {MatchId}.",
            userId,
            matchId);

        return new CallingTokenResponseDto
        {
            MatchId = matchId,
            Token = tokenResponse.Value.Token,
            ExpiresOn = tokenResponse.Value.ExpiresOn,
            CommunicationUserId = communicationUserId,
            PeerCommunicationUserId = peerCommunicationUserId
        };
    }

    private async Task<string> EnsureCommunicationUserAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(user.AcsUserId))
        {
            return user.AcsUserId;
        }

        var identityResponse = await _identityClient.CreateUserAsync(
            cancellationToken);

        user.AcsUserId = identityResponse.Value.Id;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Kunde inte skapa samtalsidentitet för användaren.");
        }

        return user.AcsUserId;
    }
}
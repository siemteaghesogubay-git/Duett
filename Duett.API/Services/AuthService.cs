using Duett.API.Data;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Duett.API.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IEmailService emailService,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _tokenService = tokenService;
    }

    public async Task RegisterAsync(RegisterDto dto)
    {
        var email = dto.Email.Trim();

        // Avslöja inte om e-postadressen redan finns.
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Kontot och rollen sparas tillsammans.
        await using (var transaction =
            await _context.Database.BeginTransactionAsync())
        {
            var createResult = await _userManager.CreateAsync(
                user,
                dto.Password);

            if (!createResult.Succeeded)
            {
                throw new ArgumentException(
                    "Kontot kunde inte skapas. Kontrollera e-postadressen " +
                    "och att lösenordet innehåller minst 8 tecken, " +
                    "stor och liten bokstav, siffra och specialtecken.");
            }

            var roleResult = await _userManager.AddToRoleAsync(
                user,
                "USER");

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Registreringen kunde inte slutföras. Försök igen senare.");
            }

            await transaction.CommitAsync();
        }

        // Skapa verifieringstoken efter att kontot sparats.
        var token = await _userManager
            .GenerateEmailConfirmationTokenAsync(user);

        // Sparar meddelandet lokalt via DevelopmentEmailService.
        await _emailService.SendVerificationAsync(
            user.Email!,
            token);
    }

    public async Task VerifyEmailAsync(VerifyEmailDto dto)
    {
        var user = await _userManager.FindByEmailAsync(
            dto.Email.Trim());

        if (user is null)
        {
            throw new ArgumentException(
                "Verifieringen är ogiltig eller har gått ut.");
        }

        var result = await _userManager.ConfirmEmailAsync(
            user,
            dto.Token);

        if (!result.Succeeded)
        {
            throw new ArgumentException(
                "Verifieringen är ogiltig eller har gått ut.");
        }
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(
            dto.Email.Trim());

        if (user is null)
        {
            return null;
        }

        // Stoppa inloggning om kontot är tillfälligt låst.
        if (await _userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        var passwordIsValid = await _userManager.CheckPasswordAsync(
            user,
            dto.Password);

        if (!passwordIsValid)
        {
            var failedResult = await _userManager.AccessFailedAsync(user);

            if (!failedResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Inloggningen kunde inte behandlas. Försök igen.");
            }

            return null;
        }

        // E-postadressen måste vara verifierad.
        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            return null;
        }

        var resetResult = await _userManager
            .ResetAccessFailedCountAsync(user);

        if (!resetResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Inloggningen kunde inte behandlas. Försök igen.");
        }

        return await _tokenService.CreateTokenAsync(user);
    }
    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(
            dto.Email.Trim());

        // Samma publika svar även om kontot saknas
        // eller e-postadressen inte är verifierad.
        if (user is null ||
            !await _userManager.IsEmailConfirmedAsync(user))
        {
            return;
        }

        var token = await _userManager
            .GeneratePasswordResetTokenAsync(user);

        await _emailService.SendPasswordResetAsync(
            user.Email!,
            token);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        const string errorMessage =
            "Lösenordet kunde inte återställas. " +
            "Kontrollera återställningsuppgifterna och lösenordskraven.";

        if (dto.NewPassword != dto.ConfirmPassword)
        {
            throw new ValidationException(
                "Lösenorden matchar inte.");
        }

        var user = await _userManager.FindByEmailAsync(
            dto.Email.Trim());

        if (user is null ||
            !await _userManager.IsEmailConfirmedAsync(user))
        {
            throw new ValidationException(errorMessage);
        }

        var result = await _userManager.ResetPasswordAsync(
            user,
            dto.Token,
            dto.NewPassword);

        if (!result.Succeeded)
        {
            throw new ValidationException(errorMessage);
        }
    }
}

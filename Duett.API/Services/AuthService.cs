using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Duett.API.Data;
using Duett.API.Dtos;
using Duett.API.Models;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Duett.API.Services;

public class AuthService : IAuthService
{
    private const int MaxVerificationAttempts = 5;

    private const string InvalidVerificationMessage =
        "The verification code is invalid or has expired. " +
        "Request a new code and try again.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IEmailService emailService,
        ITokenService tokenService,
        IDataProtectionProvider dataProtectionProvider)
    {
        _userManager = userManager;
        _context = context;
        _emailService = emailService;
        _tokenService = tokenService;
        _dataProtectionProvider = dataProtectionProvider;
    }

    public async Task RegisterAsync(RegisterDto dto)
    {
        var email = dto.Email.Trim();

        // Samma publika svar när kontot redan finns.
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var code = GenerateVerificationCode();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            CreatedAtUtc = now,
            EmailVerificationExpiresAtUtc = now.AddMinutes(10),
            EmailVerificationNextSendAtUtc = now.AddSeconds(60),
            EmailVerificationFailedAttempts = 0
        };

        user.ProtectedEmailVerificationCode =
            CreateVerificationProtector(user.Id, email).Protect(code);

        // Kontot, verifieringsuppgifterna och rollen sparas tillsammans.
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

        // Skicka först när kontot finns i databasen.
        // Om utskicket misslyckas kan användaren begära en ny kod.
        await _emailService.SendVerificationAsync(email, code);
    }

    public async Task ResendVerificationAsync(
        ResendVerificationDto dto)
    {
        var normalizedEmail = _userManager.NormalizeEmail(
            dto.Email.Trim());

        var user = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.NormalizedEmail == normalizedEmail);

        var now = DateTime.UtcNow;

        // Avslöja inte om kontot saknas eller redan är verifierat.
        if (user is null ||
            user.EmailConfirmed ||
            string.IsNullOrWhiteSpace(user.Email) ||
            user.EmailVerificationNextSendAtUtc > now)
        {
            return;
        }

        var code = GenerateVerificationCode();

        var protectedCode = CreateVerificationProtector(
            user.Id,
            user.Email).Protect(code);

        var expiresAt = now.AddMinutes(10);
        var nextSendAt = now.AddSeconds(60);
        var concurrencyStamp = Guid.NewGuid().ToString();

        // Villkoret kontrolleras i databasen.
        // Två samtidiga anrop får därför inte båda skapa en ny kod.
        var affectedRows = await _context.Users
            .Where(candidate =>
                candidate.Id == user.Id &&
                candidate.NormalizedEmail == normalizedEmail &&
                !candidate.EmailConfirmed &&
                (candidate.EmailVerificationNextSendAtUtc == null ||
                 candidate.EmailVerificationNextSendAtUtc <= now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    candidate => candidate.ProtectedEmailVerificationCode,
                    protectedCode)
                .SetProperty(
                    candidate => candidate.EmailVerificationExpiresAtUtc,
                    (DateTime?)expiresAt)
                .SetProperty(
                    candidate => candidate.EmailVerificationNextSendAtUtc,
                    (DateTime?)nextSendAt)
                .SetProperty(
                    candidate => candidate.EmailVerificationFailedAttempts,
                    0)
                .SetProperty(
                    candidate => candidate.ConcurrencyStamp,
                    concurrencyStamp));

        if (affectedRows != 1)
        {
            return;
        }

        await _emailService.SendVerificationAsync(
            user.Email,
            code);
    }

    public async Task VerifyEmailAsync(VerifyEmailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            !IsSixDigitCode(dto.Token))
        {
            throw new ArgumentException(InvalidVerificationMessage);
        }

        var normalizedEmail = _userManager.NormalizeEmail(
            dto.Email.Trim());

        // Ingen spårad användare blandas med ExecuteUpdateAsync.
        var user = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.NormalizedEmail == normalizedEmail);

        var now = DateTime.UtcNow;

        if (user is null ||
            user.EmailConfirmed ||
            string.IsNullOrWhiteSpace(user.Email) ||
            string.IsNullOrWhiteSpace(user.ProtectedEmailVerificationCode) ||
            user.EmailVerificationExpiresAtUtc is null ||
            user.EmailVerificationExpiresAtUtc <= now ||
            user.EmailVerificationFailedAttempts >= MaxVerificationAttempts)
        {
            throw new ArgumentException(InvalidVerificationMessage);
        }

        string expectedCode;

        try
        {
            expectedCode = CreateVerificationProtector(
                user.Id,
                user.Email).Unprotect(user.ProtectedEmailVerificationCode);
        }
        catch (CryptographicException)
        {
            throw new ArgumentException(InvalidVerificationMessage);
        }

        var codeMatches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedCode),
            Encoding.UTF8.GetBytes(dto.Token));

        // Samma skyddade värde identifierar den aktuella koden.
        // Om en ny kod har skapats under tiden matchar inte villkoret.
        var protectedCode = user.ProtectedEmailVerificationCode;
        var checkedAt = DateTime.UtcNow;
        var concurrencyStamp = Guid.NewGuid().ToString();

        var eligibleUser = _context.Users.Where(candidate =>
            candidate.Id == user.Id &&
            candidate.NormalizedEmail == normalizedEmail &&
            !candidate.EmailConfirmed &&
            candidate.ProtectedEmailVerificationCode == protectedCode &&
            candidate.EmailVerificationExpiresAtUtc > checkedAt &&
            candidate.EmailVerificationFailedAttempts < MaxVerificationAttempts);

        if (!codeMatches)
        {
            // Försöken räknas atomärt även vid samtidiga anrop.
            await eligibleUser.ExecuteUpdateAsync(setters => setters
                .SetProperty(
                    candidate => candidate.EmailVerificationFailedAttempts,
                    candidate => candidate.EmailVerificationFailedAttempts + 1)
                .SetProperty(
                    candidate => candidate.ConcurrencyStamp,
                    concurrencyStamp));

            throw new ArgumentException(InvalidVerificationMessage);
        }

        // Verifiera och förbruka koden i samma databasuppdatering.
        var affectedRows = await eligibleUser.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(
                    candidate => candidate.EmailConfirmed,
                    true)
                .SetProperty(
                    candidate => candidate.ProtectedEmailVerificationCode,
                    (string?)null)
                .SetProperty(
                    candidate => candidate.EmailVerificationExpiresAtUtc,
                    (DateTime?)null)
                .SetProperty(
                    candidate => candidate.EmailVerificationNextSendAtUtc,
                    (DateTime?)null)
                .SetProperty(
                    candidate => candidate.EmailVerificationFailedAttempts,
                    0)
                .SetProperty(
                    candidate => candidate.ConcurrencyStamp,
                    concurrencyStamp));

        if (affectedRows != 1)
        {
            throw new ArgumentException(InvalidVerificationMessage);
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

    private IDataProtector CreateVerificationProtector(
        string userId,
        string email)
    {
        // Koden binds till både användaren och e-postadressen.
        var normalizedEmail = _userManager.NormalizeEmail(email.Trim());

        return _dataProtectionProvider
            .CreateProtector("Duett.EmailVerificationCode.v1")
            .CreateProtector(userId)
            .CreateProtector(normalizedEmail!);
    }

    private static string GenerateVerificationCode()
    {
        return RandomNumberGenerator
            .GetInt32(0, 1_000_000)
            .ToString("D6", CultureInfo.InvariantCulture);
    }

    private static bool IsSixDigitCode(string? code)
    {
        return code is { Length: 6 } &&
            code.All(character => character >= '0' && character <= '9');
    }
}
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Duett.API.Dtos;
using Duett.API.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Duett.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDto dto)
    {
        try
        {
            await _authService.RegisterAsync(dto);

            return Ok(new
            {
                message = "Registreringsförfrågan har behandlats."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailDto dto)
    {
        try
        {
            await _authService.VerifyEmailAsync(dto);

            return Ok(new
            {
                message = "E-postadressen är verifierad."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);

            if (result is null)
            {
                return Unauthorized(new
                {
                    message =
                        "Inloggningen misslyckades. Kontrollera dina uppgifter " +
                        "och att e-postadressen är verifierad."
                });
            }

            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return Conflict(new
            {
                message =
                    "Inloggningen kunde inte behandlas. Försök igen."
            });
        }
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting("PasswordRecovery")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordDto dto)
    {
        await _authService.ForgotPasswordAsync(dto);

        return Ok(new
        {
            message =
                "Om adressen tillhör ett verifierat konto skickas " +
                "instruktioner för att återställa lösenordet."
        });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [EnableRateLimiting("PasswordRecovery")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto dto)
    {
        try
        {
            await _authService.ResetPasswordAsync(dto);

            return Ok(new
            {
                message = "Lösenordet har återställts."
            });
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Återställningen misslyckades.",
                detail: ex.Message);
        }
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var roles = User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToArray();

        return Ok(new
        {
            userId,
            roles
        });
    }
}
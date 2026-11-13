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
                message = "Your registration request has been processed."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Registration could not be completed.",
                detail: "Please try again later.");
        }
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    [EnableRateLimiting("EmailVerification")]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailDto dto)
    {
        try
        {
            await _authService.VerifyEmailAsync(dto);

            return Ok(new
            {
                message = "Your email address has been verified."
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
    [HttpPost("resend-verification")]
    [EnableRateLimiting("EmailVerification")]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationDto dto)
    {
        await _authService.ResendVerificationAsync(dto);

        // Samma svar för okänd, verifierad eller overifierad adress.
        return Ok(new
        {
            message =
                "If this address belongs to an unverified account, " +
                "a verification code will be sent. " +
                "Please wait at least 60 seconds between requests."
        });
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
                        "Sign-in failed. Check your details and make sure " +
                        "your email address is verified."
                });
            }

            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Sign-in could not be completed.",
                detail: "Please try again later.");
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
                "If this address belongs to a verified account, " +
                "password reset instructions will be sent."
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
                message = "Your password has been reset."
            });
        }
        catch (ValidationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Password reset failed.",
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
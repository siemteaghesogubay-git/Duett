using Duett.API.Dtos;

namespace Duett.API.Services.IServices;

public interface IAuthService
{

    Task RegisterAsync(RegisterDto dto);
    Task VerifyEmailAsync(VerifyEmailDto dto);
    Task<LoginResponseDto?> LoginAsync(LoginDto dto);
    Task ForgotPasswordAsync(ForgotPasswordDto dto);

    Task ResetPasswordAsync(ResetPasswordDto dto);
}
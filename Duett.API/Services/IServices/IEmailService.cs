namespace Duett.API.Services.IServices;

public interface IEmailService
{
    Task SendVerificationAsync(string email, string token);
    Task SendPasswordResetAsync(string email, string token);
}
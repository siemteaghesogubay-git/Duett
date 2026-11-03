using System.Text.Json;
using Duett.API.Services.IServices;

namespace Duett.API.Services;

public class DevelopmentEmailService : IEmailService
{
    private readonly IWebHostEnvironment _environment;

    public DevelopmentEmailService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task SendVerificationAsync(
        string email,
        string token)
    {
        return SaveMessageAsync(
            email,
            token,
            "verify-email");
    }

    public Task SendPasswordResetAsync(
        string email,
        string token)
    {
        return SaveMessageAsync(
            email,
            token,
            "reset-password");
    }

    private async Task SaveMessageAsync(
        string email,
        string token,
        string filePrefix)
    {
        if (!_environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "En riktig e-posttjänst måste konfigureras.");
        }

        var directory = Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "Mail");

        Directory.CreateDirectory(directory);

        var message = new
        {
            Email = email,
            Token = token
        };

        var json = JsonSerializer.Serialize(
            message,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        var filePath = Path.Combine(
            directory,
            $"{filePrefix}-{Guid.NewGuid():N}.json");

        await File.WriteAllTextAsync(filePath, json);
    }
}
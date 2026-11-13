using System.Net;
using Azure;
using Azure.Communication.Email;
using Duett.API.Services.IServices;

namespace Duett.API.Services;

public sealed class AzureEmailService : IEmailService
{
    private readonly EmailClient _client;
    private readonly string _senderAddress;
    private readonly string _frontendUrl;

    public AzureEmailService(
        EmailClient client,
        IConfiguration configuration)
    {
        _client = client;

        _senderAddress = configuration["Email:SenderAddress"]
            ?? throw new InvalidOperationException(
                "Email:SenderAddress saknas.");

        var frontendUrl = configuration["Email:FrontendUrl"];

        if (!Uri.TryCreate(
                frontendUrl,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException(
                "Email:FrontendUrl måste vara en giltig frontendadress " +
                "utan query eller fragment. HTTPS krävs utom för localhost.");
        }

        _frontendUrl = uri.AbsoluteUri.TrimEnd('/');
    }

    public Task SendVerificationAsync(string email, string token)
    {
        return SendAsync(
            email,
            token,
            action: "verify",
            subject: "Verify your Duett email",
            heading: "Welcome to Duett",
            instruction: "Confirm your email address to finish creating your account.",
            buttonText: "Verify email");
    }

    public Task SendPasswordResetAsync(string email, string token)
    {
        return SendAsync(
            email,
            token,
            action: "reset",
            subject: "Reset your Duett password",
            heading: "Reset your password",
            instruction: "Open the link below to choose a new password.",
            buttonText: "Reset password");
    }

    private async Task SendAsync(
        string email,
        string token,
        string action,
        string subject,
        string heading,
        string instruction,
        string buttonText)
    {
        // Fragmentet läses av React-appen.
        var link =
            $"{_frontendUrl}/#action={action}" +
            $"&email={Uri.EscapeDataString(email)}" +
            $"&token={Uri.EscapeDataString(token)}";

        var encodedLink = WebUtility.HtmlEncode(link);

        var html = $"""
            <!DOCTYPE html>
            <html lang="en">
            <body>
                <h1>{heading}</h1>
                <p>{instruction}</p>
                <p><a href="{encodedLink}">{buttonText}</a></p>
                <p>If you did not request this, you can ignore this email.</p>
                <p>Duett</p>
            </body>
            </html>
            """;

        var plainText = $"""
            {heading}

            {instruction}

            {link}

            If you did not request this, you can ignore this email.

            Duett
            """;

        var operation = await _client.SendAsync(
            wait: WaitUntil.Completed,
            senderAddress: _senderAddress,
            recipientAddress: email,
            subject: subject,
            htmlContent: html,
            plainTextContent: plainText);

        if (operation.Value.Status != EmailSendStatus.Succeeded)
        {
            throw new InvalidOperationException(
                "E-postutskicket kunde inte slutföras.");
        }
    }
}

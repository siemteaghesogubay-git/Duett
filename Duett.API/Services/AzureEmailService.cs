using System.Net;
using Azure;
using Azure.Communication.Email;
using Duett.API.Services.IServices;

namespace Duett.API.Services;

public sealed class AzureEmailService : IEmailService
{
    private readonly EmailClient _client;
    private readonly ILogger<AzureEmailService> _logger;
    private readonly string _senderAddress;
    private readonly string _frontendUrl;

    public AzureEmailService(
        EmailClient client,
        IConfiguration configuration,
        ILogger<AzureEmailService> logger)
    {
        _client = client;
        _logger = logger;

        var senderAddress = configuration["Email:SenderAddress"];

        if (string.IsNullOrWhiteSpace(senderAddress))
        {
            throw new InvalidOperationException(
                "Email:SenderAddress saknas.");
        }

        _senderAddress = senderAddress.Trim();

        var frontendUrl = configuration["Email:FrontendUrl"];

        if (string.IsNullOrWhiteSpace(frontendUrl) ||
            !Uri.TryCreate(
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
        // Parametern heter token för att matcha befintligt interface.
        // För verifiering är värdet nu en sexsiffrig kod.
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (token.Length != 6 ||
            token.Any(character => character < '0' || character > '9'))
        {
            throw new ArgumentException(
                "Verification code must contain six digits.",
                nameof(token));
        }

        var encodedCode = WebUtility.HtmlEncode(token);

        var html = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <title>Verify your Duett email</title>
            </head>
            <body>
                <h1>Welcome to Duett</h1>

                <p>Your verification code is:</p>

                <p style="font-size:32px;font-weight:bold;letter-spacing:6px">
                    {encodedCode}
                </p>

                <p>
                    Open Duett, select Verify email, and enter your
                    email address and this six-digit code.
                </p>

                <p>
                    The code expires in 10 minutes and can only be used once.
                </p>

                <p>
                    If you did not create a Duett account,
                    you can ignore this email.
                </p>

                <p>Duett</p>
            </body>
            </html>
            """;

        var plainText = $"""
            Welcome to Duett

            Your verification code is: {token}

            Open Duett, select Verify email, and enter your email address
            and this six-digit code.

            The code expires in 10 minutes and can only be used once.

            If you did not create a Duett account, you can ignore this email.

            Duett
            """;

        return SendAsync(
            email,
            "verify",
            "Verify your Duett email",
            html,
            plainText);
    }

    public Task SendPasswordResetAsync(string email, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var link =
            $"{_frontendUrl}/#action=reset" +
            $"&email={Uri.EscapeDataString(email.Trim())}" +
            $"&token={Uri.EscapeDataString(token)}";

        var encodedLink = WebUtility.HtmlEncode(link);

        var html = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <title>Reset your Duett password</title>
            </head>
            <body>
                <h1>Reset your password</h1>

                <p>Open the link below to choose a new password.</p>

                <p>
                    <a href="{encodedLink}">Reset password</a>
                </p>

                <p>
                    If you did not request this, you can ignore this email.
                </p>

                <p>Duett</p>
            </body>
            </html>
            """;

        var plainText = $"""
            Reset your password

            Open the link below to choose a new password.

            {link}

            If you did not request this, you can ignore this email.

            Duett
            """;

        return SendAsync(
            email,
            "reset",
            "Reset your Duett password",
            html,
            plainText);
    }

    private async Task SendAsync(
        string email,
        string emailType,
        string subject,
        string html,
        string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        _logger.LogInformation(
            "Startar Azure-mejl. Typ: {EmailType}.",
            emailType);

        try
        {
            var operation = await _client.SendAsync(
                wait: WaitUntil.Completed,
                senderAddress: _senderAddress,
                recipientAddress: email.Trim(),
                subject: subject,
                htmlContent: html,
                plainTextContent: plainText);

            _logger.LogInformation(
                "Azure-mejl behandlat. OperationId: {OperationId}, " +
                "Status: {Status}.",
                operation.Id,
                operation.Value.Status);

            if (operation.Value.Status != EmailSendStatus.Succeeded)
            {
                throw new InvalidOperationException(
                    "E-postutskicket kunde inte slutföras.");
            }
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                "Azure-mejl misslyckades. HTTP-status: {Status}, " +
                "Felkod: {ErrorCode}.",
                ex.Status,
                ex.ErrorCode);

            throw;
        }
    }
}
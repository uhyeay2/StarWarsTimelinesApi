using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using StarWarsTimelines.Application;
using StarWarsTimelines.Application.Abstractions;

namespace StarWarsTimelines.Api;

/// <summary>
/// SMTP-backed implementation of <see cref="IEmailSender"/> using MailKit.
/// </summary>
/// <remarks>
/// When no SMTP host is configured, or when no SMTP password (the Resend API key) is configured in a development
/// environment, the message is written to the application log instead of being delivered, so development flows (such
/// as email verification) remain usable locally without mail credentials. Delivery failures are logged and swallowed
/// so that email problems never break the business operation that triggered the message.
/// </remarks>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SmtpEmailSender> _logger;

    /// <summary>
    /// Creates a new instance of the <see cref="SmtpEmailSender"/>.
    /// </summary>
    /// <param name="options">The email configuration options.</param>
    /// <param name="environment">The hosting environment used to decide whether missing SMTP credentials fall back to logging.</param>
    /// <param name="logger">The logger used to report delivery status and failures.</param>
    public SmtpEmailSender(EmailOptions options, IHostEnvironment environment, ILogger<SmtpEmailSender> logger)
    {
        _options = options;
        _environment = environment;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var missingCredential =
            string.IsNullOrWhiteSpace(_options.SmtpHost)
            || (string.IsNullOrWhiteSpace(_options.SmtpPassword) && _environment.IsDevelopment());

        if (missingCredential)
        {
            var missing = string.IsNullOrWhiteSpace(_options.SmtpHost) ? "host" : "password";
            _logger.LogInformation(
                "Email '{Subject}' to {ToAddress} was not sent because no SMTP {Missing} is configured.{NewLine}{Body}",
                subject,
                toAddress,
                missing,
                Environment.NewLine,
                htmlBody);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(string.Empty, toAddress));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(
                _options.SmtpHost,
                _options.SmtpPort,
                _options.SmtpEnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
            {
                await client.AuthenticateAsync(_options.SmtpUsername, _options.SmtpPassword, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email '{Subject}' sent to {ToAddress}.", subject, toAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email '{Subject}' to {ToAddress}.", subject, toAddress);
        }
    }
}

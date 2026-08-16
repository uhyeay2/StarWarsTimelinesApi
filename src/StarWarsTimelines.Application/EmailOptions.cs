namespace StarWarsTimelines.Application;

/// <summary>
/// Configuration options for transactional emails, including the SMTP server used to deliver them.
/// </summary>
/// <param name="FromAddress">The sender email address shown to recipients.</param>
/// <param name="FromName">The sender display name shown to recipients.</param>
/// <param name="SmtpHost">The SMTP server host name. When empty, emails are not delivered; the message is written
/// to the application log instead so development flows remain usable without mail infrastructure.</param>
/// <param name="SmtpPort">The SMTP server port.</param>
/// <param name="SmtpUsername">The optional SMTP authentication user name.</param>
/// <param name="SmtpPassword">The optional SMTP authentication password.</param>
/// <param name="SmtpEnableSsl">Whether to connect to the SMTP server over TLS.</param>
/// <param name="VerificationUrl">The base URL of the email verification page, which receives the verification token
/// as a <c>token</c> query string parameter.</param>
public sealed record EmailOptions(
    string FromAddress,
    string FromName,
    string SmtpHost,
    int SmtpPort,
    string SmtpUsername,
    string SmtpPassword,
    bool SmtpEnableSsl,
    string VerificationUrl);

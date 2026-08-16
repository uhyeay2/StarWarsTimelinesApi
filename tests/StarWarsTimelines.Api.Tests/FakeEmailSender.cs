using System.Collections.Concurrent;
using StarWarsTimelines.Application.Abstractions;

namespace StarWarsTimelines.Api.Tests;

/// <summary>
/// In-memory <see cref="IEmailSender"/> that captures sent messages so tests can inspect recipients and bodies
/// (for example, to extract an email verification link).
/// </summary>
public sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    /// <summary>
    /// Gets the messages captured so far, in sending order.
    /// </summary>
    public IReadOnlyList<SentEmail> Sent => _sent.ToArray();

    /// <inheritdoc />
    public Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(toAddress, subject, htmlBody));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Describes a captured email message.
    /// </summary>
    /// <param name="To">The recipient's email address.</param>
    /// <param name="Subject">The subject line of the message.</param>
    /// <param name="HtmlBody">The HTML body of the message.</param>
    public readonly record struct SentEmail(string To, string Subject, string HtmlBody);
}

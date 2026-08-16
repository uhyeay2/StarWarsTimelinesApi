namespace StarWarsTimelines.Application.Abstractions;

/// <summary>
/// Sends HTML email messages to a single recipient.
/// </summary>
/// <remarks>
/// Implementations must not throw for delivery failures: send errors are logged by the sender itself so that an
/// email outage never rolls back or blocks the underlying business operation (for example, account registration).
/// </remarks>
public interface IEmailSender
{
    /// <summary>
    /// Sends an HTML email message.
    /// </summary>
    /// <param name="toAddress">The recipient's email address.</param>
    /// <param name="subject">The subject line of the message.</param>
    /// <param name="htmlBody">The HTML body of the message.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that completes when the message has been sent or the failure has been logged.</returns>
    Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

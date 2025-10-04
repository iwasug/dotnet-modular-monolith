namespace ModularMonolith.Shared.Interfaces;

/// <summary>
/// Email service interface for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (HTML or plain text)</param>
    /// <param name="isHtml">Indicates whether the body is HTML</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends an email to multiple recipients asynchronously
    /// </summary>
    /// <param name="recipients">List of recipient email addresses</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (HTML or plain text)</param>
    /// <param name="isHtml">Indicates whether the body is HTML</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmailAsync(IEnumerable<string> recipients, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends an email with attachments asynchronously
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (HTML or plain text)</param>
    /// <param name="attachments">Dictionary of attachment file names and their byte content</param>
    /// <param name="isHtml">Indicates whether the body is HTML</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmailWithAttachmentsAsync(string to, string subject, string body, IDictionary<string, byte[]> attachments, bool isHtml = true, CancellationToken cancellationToken = default);
}

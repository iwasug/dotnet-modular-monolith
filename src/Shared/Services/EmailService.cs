using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using ModularMonolith.Shared.Common;
using ModularMonolith.Shared.Interfaces;

namespace ModularMonolith.Shared.Services;

/// <summary>
/// Email service implementation using MailKit and MimeKit
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> emailOptions, ILogger<EmailService> logger)
    {
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        await SendEmailAsync(new[] { to }, subject, body, isHtml, cancellationToken);
    }

    public async Task SendEmailAsync(IEnumerable<string> recipients, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = CreateEmailMessage(recipients, subject, body, isHtml);
            await SendMessageAsync(message, cancellationToken);
            _logger.LogInformation("Email sent successfully to {Recipients}", string.Join(", ", recipients));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}", string.Join(", ", recipients));
            throw;
        }
    }

    public async Task SendEmailWithAttachmentsAsync(string to, string subject, string body, IDictionary<string, byte[]> attachments, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = CreateEmailMessage(new[] { to }, subject, body, isHtml);
            
            foreach (var attachment in attachments)
            {
                var attachmentPart = new MimePart()
                {
                    Content = new MimeContent(new MemoryStream(attachment.Value)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = attachment.Key
                };
                
                if (message.Body is Multipart multipart)
                {
                    multipart.Add(attachmentPart);
                }
                else
                {
                    var newMultipart = new Multipart("mixed")
                    {
                        message.Body,
                        attachmentPart
                    };
                    message.Body = newMultipart;
                }
            }
            
            await SendMessageAsync(message, cancellationToken);
            _logger.LogInformation("Email with attachments sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachments to {To}", to);
            throw;
        }
    }

    private MimeMessage CreateEmailMessage(IEnumerable<string> recipients, string subject, string body, bool isHtml)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailOptions.FromName, _emailOptions.FromEmail));
        
        foreach (var recipient in recipients)
        {
            message.To.Add(MailboxAddress.Parse(recipient));
        }
        
        message.Subject = subject;
        
        var bodyBuilder = new BodyBuilder();
        if (isHtml)
        {
            bodyBuilder.HtmlBody = body;
        }
        else
        {
            bodyBuilder.TextBody = body;
        }
        
        message.Body = bodyBuilder.ToMessageBody();
        
        return message;
    }

    private async Task SendMessageAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();
        
        try
        {
            await client.ConnectAsync(_emailOptions.Host, _emailOptions.Port, _emailOptions.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
            
            if (!string.IsNullOrEmpty(_emailOptions.Username) && !string.IsNullOrEmpty(_emailOptions.Password))
            {
                await client.AuthenticateAsync(_emailOptions.Username, _emailOptions.Password, cancellationToken);
            }
            
            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}

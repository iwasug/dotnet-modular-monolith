namespace ModularMonolith.Shared.Common;

/// <summary>
/// Email configuration options
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";
    
    /// <summary>
    /// SMTP server host
    /// </summary>
    public string Host { get; set; } = string.Empty;
    
    /// <summary>
    /// SMTP server port
    /// </summary>
    public int Port { get; set; } = 587;
    
    /// <summary>
    /// Username for SMTP authentication
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Password for SMTP authentication
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Default sender email address
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Default sender name
    /// </summary>
    public string FromName { get; set; } = string.Empty;
    
    /// <summary>
    /// Enable SSL/TLS
    /// </summary>
    public bool UseSsl { get; set; } = true;
}

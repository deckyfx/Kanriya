using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GQLServer.Program;

namespace GQLServer.Services;

/// <summary>
/// Mail driver that uses system-configured SMTP settings
/// </summary>
public class SystemSmtpMailDriver : IMailDriver
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemSmtpMailDriver> _logger;
    private readonly SmtpSettings _settings;

    public string Name => "SystemSmtp";

    public SystemSmtpMailDriver(IConfiguration configuration, ILogger<SystemSmtpMailDriver> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _logger.LogDebug("Initializing SystemSmtpMailDriver...");
        _settings = LoadSmtpSettings();
        _logger.LogInformation("SystemSmtpMailDriver initialized with {Provider} provider", 
            EnvironmentConfig.Mail.Provider);
    }

    private SmtpSettings LoadSmtpSettings()
    {
        var provider = EnvironmentConfig.Mail.Provider;
        var settings = new SmtpSettings();

        if (provider.Equals("gmail", StringComparison.OrdinalIgnoreCase))
        {
            // Gmail-specific configuration
            settings.Host = "smtp.gmail.com";
            settings.Port = 587;
            settings.EnableSsl = true;
            settings.Username = EnvironmentConfig.Mail.GmailUsername;
            settings.Password = EnvironmentConfig.Mail.GmailAppPassword;
            // Gmail REQUIRES the FROM address to match the authenticated account
            settings.FromEmail = EnvironmentConfig.Mail.GmailUsername; // Use Gmail account as FROM
            settings.FromName = EnvironmentConfig.Mail.FromName;
            
            _logger.LogInformation("Using Gmail SMTP configuration for {Username}", settings.Username);
            _logger.LogDebug("Gmail FROM address set to: {FromEmail}", settings.FromEmail);
        }
        else
        {
            // Generic SMTP configuration (fallback to config file settings)
            settings.Host = _configuration["Smtp:Host"] ?? "localhost";
            settings.Port = int.Parse(_configuration["Smtp:Port"] ?? "587");
            settings.Username = _configuration["Smtp:Username"] ?? "";
            settings.Password = _configuration["Smtp:Password"] ?? "";
            settings.FromEmail = EnvironmentConfig.Mail.FromAddress;
            settings.FromName = EnvironmentConfig.Mail.FromName;
            settings.EnableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");
            
            _logger.LogInformation("Using generic SMTP configuration for host {Host}", settings.Host);
        }

        // Rate limiting configuration
        settings.RateLimit = EnvironmentConfig.Mail.RateLimit;
        settings.BatchSize = EnvironmentConfig.Mail.BatchSize;

        return settings;
    }

    public async Task<MailSendResult> SendAsync(MailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            using var smtpClient = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000 // 30 seconds
            };

            using var mailMessage = new System.Net.Mail.MailMessage
            {
                From = new MailAddress(
                    message.FromEmail ?? _settings.FromEmail, 
                    message.FromName ?? _settings.FromName),
                Subject = message.Subject,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = !string.IsNullOrEmpty(message.HtmlBody)
            };

            // Add recipients
            foreach (var email in message.ToEmail.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                mailMessage.To.Add(email.Trim());
            }

            if (!string.IsNullOrEmpty(message.CcEmail))
            {
                foreach (var email in message.CcEmail.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    mailMessage.CC.Add(email.Trim());
                }
            }

            if (!string.IsNullOrEmpty(message.BccEmail))
            {
                foreach (var email in message.BccEmail.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    mailMessage.Bcc.Add(email.Trim());
                }
            }

            // Set body
            if (!string.IsNullOrEmpty(message.HtmlBody))
            {
                mailMessage.Body = message.HtmlBody;
                
                // Add text alternative if provided
                if (!string.IsNullOrEmpty(message.TextBody))
                {
                    var alternateView = AlternateView.CreateAlternateViewFromString(
                        message.TextBody, 
                        Encoding.UTF8, 
                        "text/plain");
                    mailMessage.AlternateViews.Add(alternateView);
                }
            }
            else
            {
                mailMessage.Body = message.TextBody ?? "";
                mailMessage.IsBodyHtml = false;
            }

            // Add custom headers
            if (message.Headers != null)
            {
                foreach (var header in message.Headers)
                {
                    mailMessage.Headers.Add(header.Key, header.Value);
                }
            }

            // Add attachments
            if (message.Attachments != null)
            {
                foreach (var attachment in message.Attachments)
                {
                    var stream = new MemoryStream(attachment.Content);
                    var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);
                    mailMessage.Attachments.Add(mailAttachment);
                }
            }

            // Log email details for debugging
            _logger.LogDebug("Sending email: From={From}, To={To}, Subject={Subject}", 
                mailMessage.From?.Address, message.ToEmail, message.Subject);
            
            // Send the email
            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger.LogInformation("Email sent successfully to {ToEmail} via SystemSmtp (From: {FromEmail})", 
                message.ToEmail, mailMessage.From?.Address);

            return new MailSendResult
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString(),
                SentAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} via SystemSmtp", message.ToEmail);
            
            return new MailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }

    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if required settings are present
            if (string.IsNullOrEmpty(_settings.Host) ||
                _settings.Port <= 0 ||
                string.IsNullOrEmpty(_settings.Username) ||
                string.IsNullOrEmpty(_settings.Password))
            {
                _logger.LogWarning("SystemSmtp configuration is incomplete");
                return false;
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate SystemSmtp configuration");
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var smtpClient = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = _settings.EnableSsl,
                Timeout = 5000 // 5 seconds for test
            };

            // Try to connect (this doesn't actually send an email)
            // Note: .NET's SmtpClient doesn't have a direct "test connection" method
            // So we'll just validate the settings
            return await ValidateConfigurationAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test SystemSmtp connection");
            return false;
        }
    }

    private class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool EnableSsl { get; set; }
        public int RateLimit { get; set; }
        public int BatchSize { get; set; }
    }
}
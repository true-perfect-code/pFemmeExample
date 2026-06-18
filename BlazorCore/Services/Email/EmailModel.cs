using System;

namespace BlazorCore.Services.Email
{
    /// <summary>
    /// Represents SMTP server configuration for email delivery.
    /// Used primarily in server-side implementations (Blazor Server, WebAPI).
    /// </summary>
    public class SmtpSettingsModel
    {
        /// <summary>Gets or sets the SMTP server hostname or IP address.</summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>Gets or sets the SMTP server port (typically 25, 465, or 587).</summary>
        public int Port { get; set; }

        /// <summary>Gets or sets the username for SMTP authentication.</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>Gets or sets the password for SMTP authentication.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Gets or sets the "From" email address.</summary>
        public string From { get; set; } = string.Empty;

        /// <summary>Gets or sets a value indicating whether to use SSL/TLS encryption.</summary>
        public bool EnableSsl { get; set; }
    }

    /// <summary>
    /// Represents an email message to be sent.
    /// </summary>
    public class EmailMessageModel
    {
        /// <summary>Gets or sets the recipient's email address.</summary>
        public string ToEmail { get; set; } = string.Empty;

        /// <summary>Gets or sets the email subject line.</summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>Gets or sets the email body content (supports HTML if enabled by implementation).</summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>Gets or sets an optional reply-to email address.</summary>
        public string ReplyTo { get; set; } = string.Empty;
    }

    public class ContactformModel
    {
        public string NameEmail = string.Empty;
        public string Title = string.Empty;
        public string Message = string.Empty;

        public void Reset()
        {
            NameEmail = string.Empty;
            Title = string.Empty;
            Message = string.Empty;
        }
    }
}
using BlazorCore.Services.Dam;
using BlazorCore.Services.Email;
using BlazorCore.Services.SqlClient;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace pFemmeExample.Web.Services
{
    /// <summary>
    /// Email service implementation for Blazor Server (Web) platform.
    /// Sends emails directly via SMTP server using MailKit.
    /// </summary>
    public class Email : IEmailBase
    {
        private readonly SmtpSettingsModel _smtp;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="Email"/> class.
        /// </summary>
        /// <param name="smtpOptions">SMTP configuration from appsettings.json or similar.</param>
        public Email(IOptions<SmtpSettingsModel> smtpOptions, IServiceProvider serviceProvider)
        {
            _smtp = smtpOptions.Value;
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public async Task SendEmailAsync(EmailMessageModel data)
        {
            var message = new MimeMessage();

            // Sender (fixed from address)
            message.From.Add(new MailboxAddress(
                $"Website contact form {pFemmeExample.Shared.Global.Configuration.ConfigGeneral.ApplicationName}",
                _smtp.From));

            // Recipient
            message.To.Add(new MailboxAddress(string.Empty, data.ToEmail));

            // Subject
            message.Subject = data.Subject;

            // Reply-to (customer email address)
            if (!string.IsNullOrWhiteSpace(data.ReplyTo))
            {
                message.ReplyTo.Add(new MailboxAddress("Customer", data.ReplyTo));
            }

            // Body (HTML)
            message.Body = new TextPart("html")
            {
                Text = BuildEmailBody(data)
            };

            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.SslOnConnect);
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new Exception($"SMTP Error: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public async Task<ScalarModel> SendFeedbackAsync(ContactformModel contactForm)
        {
            ScalarModel result = new();

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "SaveFeedback>>AppMessages" },
                    { "@DisplayName", contactForm.NameEmail },
                    { "@Title", contactForm.Title },
                    { "@Body", contactForm.Message },
                    { DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString() } // Nur auf Cloud speichern
                };

                var _dam = _serviceProvider.GetRequiredService<IDamBase>();
                result = await _dam.Save(db_para)!;
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Builds an HTML email body from the message data.
        /// </summary>
        /// <param name="data">The email message data.</param>
        /// <returns>Formatted HTML email body.</returns>
        private static string BuildEmailBody(EmailMessageModel data)
        {
            return $@"
                <h3>New Contact Message</h3>
                <p><strong>Email:</strong> {data.ReplyTo}</p>
                <p><strong>Title:</strong> {data.Subject}</p>
                <p><strong>Message:</strong><br/>{data.Body}</p>
            ";
        }
    }
}
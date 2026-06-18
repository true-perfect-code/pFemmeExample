using BlazorCore.Services.Dam;
using BlazorCore.Services.Email;
using BlazorCore.Services.SqlClient;

namespace pFemmeExample.Shared.Services.Email
{
    /// <summary>
    /// Email service implementation for WASM, Capacitor, and PWA platforms.
    /// Sends emails via anonymous API endpoint (no authentication required).
    /// </summary>
    public class Email : IEmailBase
    {
        private readonly IDamBase _dam;

        /// <summary>
        /// Initializes a new instance of the <see cref="Email"/> class.
        /// </summary>
        /// <param name="dam">Data Access Manager for API calls.</param>
        public Email(IDamBase dam)
        {
            _dam = dam;
        }

        /// <inheritdoc />
        public async Task SendEmailAsync(EmailMessageModel data)
        {
            try
            {
                var smtpParameters = new Dictionary<string, string>
                {
                    { "@Case_", "SendEmail" },
                    { "ToEmail", data.ToEmail },
                    { "Subject", data.Subject },
                    { "Body", BuildEmailBody(data) },
                    { "ReplyTo", data.ReplyTo },
                };

                var result = await _dam.AnonymousQuery(smtpParameters);

                if (result == null || !string.IsNullOrEmpty(result.out_err))
                {
                    var errorMessage = result == null
                        ? "SendEmailAsync returned null result"
                        : result.out_err;

                    throw new Exception($"SMTP Error: {errorMessage}");
                }
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

                result = await _dam.AnonymousQuery(db_para)!;
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
using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.Email
{
    /// <summary>
    /// Provides email sending capabilities across all platforms.
    /// Implementations are platform-specific (SMTP server-side, client-side API, etc.).
    /// </summary>
    public interface IEmailBase
    {
        /// <summary>
        /// Sends an email message asynchronously.
        /// </summary>
        /// <param name="data">The email message containing recipient, subject, and body.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendEmailAsync(EmailMessageModel data);

        /// <summary>
        /// Sends feedback from a contact form.
        /// Stores the feedback in the database and sends an email notification.
        /// </summary>
        /// <param name="contactForm">The contact form data (name, title, message).</param>
        /// <returns>ScalarModel with success status and optional error message.</returns>
        Task<ScalarModel> SendFeedbackAsync(ContactformModel contactForm);
    }
}
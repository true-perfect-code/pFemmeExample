using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorCore.Services.Authentication
{
    /// <summary>
    /// AOT-safe dynamic validation validator for EditContext.
    /// Performs explicit, reflection-free validation on LoginModel properties.
    /// </summary>
    public class DynamicValidationValidator : ComponentBase, IDisposable
    {
        [CascadingParameter]
        private EditContext? EditContext { get; set; }

        /// <summary>Gets or sets the error message when no language is selected.</summary>
        [Parameter]
        public string TextSelectLanguage { get; set; } = "Please select a language.";

        /// <summary>Gets or sets the error message when account field is empty.</summary>
        [Parameter]
        public string TextAccountRequired { get; set; } = "Account is required.";

        /// <summary>Gets or sets the error message when password field is empty.</summary>
        [Parameter]
        public string TextPasswordRequired { get; set; } = "Password is required.";

        private ValidationMessageStore? _messageStore;

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            if (EditContext == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(DynamicValidationValidator)} requires a cascading parameter of type {nameof(EditContext)}.");
            }

            _messageStore = new ValidationMessageStore(EditContext);
            EditContext.OnValidationRequested += HandleValidationRequested;
            EditContext.OnFieldChanged += HandleFieldChanged;
        }

        /// <summary>
        /// Handles the EditContext validation request event.
        /// Clears all messages and validates the entire model.
        /// </summary>
        private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e)
        {
            if (EditContext == null || _messageStore == null)
                return;

            _messageStore.Clear();
            ValidateModel();
        }

        /// <summary>
        /// Handles the EditContext field changed event.
        /// Clears messages for the specific field and validates only that field.
        /// </summary>
        private void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
        {
            if (EditContext == null || _messageStore == null)
                return;

            _messageStore.Clear(e.FieldIdentifier);
            ValidateField(e.FieldIdentifier);
        }

        /// <summary>
        /// Validates all properties of the model.
        /// AOT-safe: uses explicit property validation, no reflection.
        /// </summary>
        private void ValidateModel()
        {
            if (_messageStore == null || EditContext == null)
                return;

            if (EditContext.Model is not LoginModel loginModel)
                return;

            ValidateAccount(loginModel.Account, nameof(LoginModel.Account));
            ValidatePassword(loginModel.Password, nameof(LoginModel.Password));
        }

        /// <summary>
        /// Validates a specific field identified by FieldIdentifier.
        /// </summary>
        private void ValidateField(FieldIdentifier fieldIdentifier)
        {
            if (_messageStore == null || EditContext == null)
                return;

            if (EditContext.Model is not LoginModel loginModel)
                return;

            if (fieldIdentifier.FieldName == nameof(LoginModel.Language))
                ValidateLanguage(loginModel.Language, fieldIdentifier);
            else if (fieldIdentifier.FieldName == nameof(LoginModel.Account))
                ValidateAccount(loginModel.Account, fieldIdentifier);
            else if (fieldIdentifier.FieldName == nameof(LoginModel.Password))
                ValidatePassword(loginModel.Password, fieldIdentifier);
        }

        // =====================================================================
        // LANGUAGE VALIDATION
        // =====================================================================

        /// <summary>
        /// Validates the language field by property name (full model validation).
        /// </summary>
        private void ValidateLanguage(string? value, string propertyName)
        {
            if (_messageStore == null || EditContext == null)
                return;

            if (string.IsNullOrEmpty(value))
            {
                var fieldIdentifier = new FieldIdentifier(EditContext.Model, propertyName);
                _messageStore.Add(fieldIdentifier, TextSelectLanguage);
            }
        }

        /// <summary>
        /// Validates the language field by FieldIdentifier (single field validation).
        /// </summary>
        private void ValidateLanguage(string? value, FieldIdentifier fieldIdentifier)
        {
            if (_messageStore == null)
                return;

            if (string.IsNullOrEmpty(value))
                _messageStore.Add(fieldIdentifier, TextSelectLanguage);
        }

        // =====================================================================
        // ACCOUNT VALIDATION
        // =====================================================================

        /// <summary>
        /// Validates the account field by property name (full model validation).
        /// </summary>
        private void ValidateAccount(string? value, string propertyName)
        {
            if (_messageStore == null || EditContext == null)
                return;

            if (string.IsNullOrEmpty(value))
            {
                var fieldIdentifier = new FieldIdentifier(EditContext.Model, propertyName);
                _messageStore.Add(fieldIdentifier, TextAccountRequired);
            }
        }

        /// <summary>
        /// Validates the account field by FieldIdentifier (single field validation).
        /// </summary>
        private void ValidateAccount(string? value, FieldIdentifier fieldIdentifier)
        {
            if (_messageStore == null)
                return;

            if (string.IsNullOrEmpty(value))
                _messageStore.Add(fieldIdentifier, TextAccountRequired);
        }

        // =====================================================================
        // PASSWORD VALIDATION
        // =====================================================================

        /// <summary>
        /// Validates the password field by property name (full model validation).
        /// </summary>
        private void ValidatePassword(string? value, string propertyName)
        {
            if (_messageStore == null || EditContext == null)
                return;

            if (string.IsNullOrEmpty(value))
            {
                var fieldIdentifier = new FieldIdentifier(EditContext.Model, propertyName);
                _messageStore.Add(fieldIdentifier, TextPasswordRequired);
            }
        }

        /// <summary>
        /// Validates the password field by FieldIdentifier (single field validation).
        /// </summary>
        private void ValidatePassword(string? value, FieldIdentifier fieldIdentifier)
        {
            if (_messageStore == null)
                return;

            if (string.IsNullOrEmpty(value))
                _messageStore.Add(fieldIdentifier, TextPasswordRequired);
        }

        // =====================================================================
        // DISPOSAL
        // =====================================================================

        /// <inheritdoc />
        public void Dispose()
        {
            if (EditContext != null)
            {
                EditContext.OnValidationRequested -= HandleValidationRequested;
                EditContext.OnFieldChanged -= HandleFieldChanged;
            }
            GC.SuppressFinalize(this);
        }
    }
}
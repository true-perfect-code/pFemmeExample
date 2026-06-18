using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorCore.Services.Authentication
{
    public class DynamicValidationValidator : ComponentBase, IDisposable
    {
        [CascadingParameter] private EditContext? EditContext { get; set; }
        [Parameter] public string TextSelectLanguage { get; set; } = "Please select a language.";
        [Parameter] public string TextAccountRequired { get; set; } = "Account is required.";
        [Parameter] public string TextPasswordRequired { get; set; } = "Password is required.";
        private ValidationMessageStore? _messageStore;

        protected override void OnInitialized()
        {
            if (EditContext == null)
                throw new InvalidOperationException($"{nameof(DynamicValidationValidator)} requires a cascading parameter of type {nameof(EditContext)}.");

            _messageStore = new ValidationMessageStore(EditContext);
            EditContext.OnValidationRequested += HandleValidationRequested;
            EditContext.OnFieldChanged += HandleFieldChanged;
        }

        private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e)
        {
            if (EditContext == null || _messageStore == null) return;
            _messageStore.Clear();
            ValidateModel();
        }

        private void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
        {
            if (EditContext == null || _messageStore == null) return;
            _messageStore.Clear(e.FieldIdentifier);
            ValidateField(e.FieldIdentifier);
        }

        // AOT-sichere Validierung ohne Reflection
        private void ValidateModel()
        {
            if (_messageStore == null || EditContext == null) return;
            if (EditContext.Model is not LoginModel loginModel) return;

            // Explizite, statische Validierung - 100% AOT-sicher
            //ValidateLanguage(loginModel.Language, nameof(LoginModel.Language));
            ValidateAccount(loginModel.Account, nameof(LoginModel.Account));
            ValidatePassword(loginModel.Password, nameof(LoginModel.Password));
        }

        private void ValidateField(FieldIdentifier fieldIdentifier)
        {
            if (_messageStore == null || EditContext == null) return;
            if (EditContext.Model is not LoginModel loginModel) return;

            // FieldIdentifier basierte Validierung
            if (fieldIdentifier.FieldName == nameof(LoginModel.Language))
                ValidateLanguage(loginModel.Language, fieldIdentifier);
            else if (fieldIdentifier.FieldName == nameof(LoginModel.Account))
                ValidateAccount(loginModel.Account, fieldIdentifier);
            else if (fieldIdentifier.FieldName == nameof(LoginModel.Password))
                ValidatePassword(loginModel.Password, fieldIdentifier);
        }

        // Überladene Methoden für beide Validierungsarten
        private void ValidateLanguage(string? value, string propertyName)
        {
            if (_messageStore == null || EditContext == null) return;
            if (string.IsNullOrEmpty(value))
            {
                var fieldIdentifier = new FieldIdentifier(EditContext.Model, propertyName);
                _messageStore.Add(fieldIdentifier, TextSelectLanguage);
            }
        }

        private void ValidateLanguage(string? value, FieldIdentifier fieldIdentifier)
        {
            if (_messageStore == null) return;
            if (string.IsNullOrEmpty(value))
                _messageStore.Add(fieldIdentifier, TextSelectLanguage);
        }

        private void ValidateAccount(string? value, string propertyName)
        {
            if (_messageStore == null || EditContext == null) return;
            if (string.IsNullOrEmpty(value))
            {
                var fieldIdentifier = new FieldIdentifier(EditContext.Model, propertyName);
                _messageStore.Add(fieldIdentifier, TextAccountRequired);
            }
        }

        private void ValidateAccount(string? value, FieldIdentifier fieldIdentifier)
        {
            if (_messageStore == null) return;
            if (string.IsNullOrEmpty(value))
                _messageStore.Add(fieldIdentifier, TextAccountRequired);
        }

        private void ValidatePassword(string? value, string propertyName)
        {
            if (_messageStore == null || EditContext == null) return;
            if (string.IsNullOrEmpty(value))
            {
                var fieldIdentifier = new FieldIdentifier(EditContext.Model, propertyName);
                _messageStore.Add(fieldIdentifier, TextPasswordRequired);
            }
        }

        private void ValidatePassword(string? value, FieldIdentifier fieldIdentifier)
        {
            if (_messageStore == null) return;
            if (string.IsNullOrEmpty(value))
                _messageStore.Add(fieldIdentifier, TextPasswordRequired);
        }

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

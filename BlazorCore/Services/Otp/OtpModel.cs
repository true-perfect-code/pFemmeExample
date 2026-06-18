namespace BlazorCore.Services.Otp
{
    // Enthält alle Daten für die Anzeige des QR-Code-Setups
    public class OtpSetupDataModel
    {
        public string QrSvg { get; set; } = string.Empty;
        public string OtpUri { get; set; } = string.Empty;
        public string BackupCode { get; set; } = string.Empty;
    }

    public class OtpModel : OtpSetupDataModel
    {
        // Request
        public string AuthUsers_ID { get; set; } = string.Empty;
        public string UserInputCode { get; set; } = string.Empty;

        // Response
        public bool success { get; set; }
        public string secret { get; set; } = string.Empty;

        public string err { get; set; } = string.Empty;
    }

    public class OtpParametersModel : Authentication.AccountPasswordModel
    {
        public string UnixTS { get; set; } = string.Empty;
        public string AuthUsers_UnixTS { get; set; } = string.Empty;
        public string OtpBackupCode { get; set; } = string.Empty;
        public string OtpUserDigitInput { get; set; } = string.Empty;

        public void Reset()
        {
            AuthUsers_UnixTS = string.Empty;
            Account = string.Empty;
            Password = string.Empty;
            PasswordRepeat = string.Empty;
            OtpBackupCode = string.Empty;
            OtpUserDigitInput = string.Empty;
        }
    }

    // Das Ergebnismodell, das das OnClose-Event zurückgibt
    public class OtpSetupResultModel
    {
        public bool IsAccepted { get; set; }
    }

    public class OtpStatus
    {
        public bool ResetRequested { get; set; } = false;
    }

    public class OtpRevokeTextModel
    {
        private readonly Services.AppState.IAppStateBase _appState;

        public OtpRevokeTextModel(Services.AppState.IAppStateBase appState)
        {
            _appState = appState;
        }

        public string Title => _appState.T("Deactivate 2FA"); // { get; set; } = "Deactivate 2FA";
        public string Description => _appState.T("To deactivate two-factor authentication, please confirm your account and password."); // { get; set; } = "To deactivate two-factor authentication, please confirm your account and password.";
        public string AccountLabel => _appState.T("Account"); // { get; set; } = "Account";
        public string PasswordLabel => _appState.T("Password"); // { get; set; } = "Password";
        public string RepeatPasswordLabel => _appState.T("Repeat Password"); // { get; set; } = "Repeat Password";
        public string BackupCodeLabel => _appState.T("Backup Code"); // { get; set; } = "Backup Code";
        public string BackupCodeDescription => _appState.T("Enter the 16-digit backup code generated during 2FA setup."); // { get; set; } = "Enter the 16-digit backup code generated during 2FA setup.";
        public string RevokeButtonText => _appState.T("Deactivate 2FA"); // { get; set; } = "Deactivate 2FA";
        public string CancelButtonText => _appState.T("Cancel"); // { get; set; } = "Cancel";

        public string SuccessMessageTitle => _appState.T("Success"); // { get; set; } = "Success";
        public string SuccessMessage => _appState.T("2FA has been successfully disabled!"); // { get; set; } = "2FA has been successfully disabled!";
        public string ErrorMessageTitle => _appState.T("Error"); // { get; set; } = "Error";
        public string ErrorMessage => _appState.T("2FA Deactivation failed. Username, password, or backup code are incorrect!"); // { get; set; } = "2FA Deactivation failed. Username, password, or backup code are incorrect!";

        public string AllFieldsRequiredError => _appState.T("Please fill in all fields."); // { get; set; } = "Please fill in all fields.";
        public string PasswordsDoNotMatchError => _appState.T("Passwords do not match."); // { get; set; } = "Passwords do not match.";
    }

}

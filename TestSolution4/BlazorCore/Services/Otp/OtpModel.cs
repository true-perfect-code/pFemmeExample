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

        //// QR Code
        //public string QrSvg { get; set; } = string.Empty;
        //public string otpUri { get; set; } = string.Empty;
        //public string BackupCode { get; set; } = string.Empty;

        public string err { get; set; } = string.Empty;
    }

    public class OtpParametersModel : Authentication.AccountPasswordModel
    {
        public string UnixTS { get; set; } = string.Empty;
        public string AuthUsers_UnixTS { get; set; } = string.Empty;

        //public string Account { get; set; } = string.Empty;
        //public string Password { get; set; } = string.Empty;
        //public string PasswordRepeat { get; set; } = string.Empty;
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

    //// Enthält alle Texte für die Lokalisierung der Komponente
    //public class OtpSetupTextModel
    //{
    //    private readonly Services.AppState.IAppStateBase _appState;

    //    public OtpSetupTextModel(Services.AppState.IAppStateBase appState)
    //    {
    //        _appState = appState;
    //    }

    //    public string Title => _appState.T("QR code for 2FA"); // { get; set; } = "QR code for 2FA";
    //    public string Description => _appState.T("Scan this QR code with your authentication app..."); // { get; set; } = "Scan this QR code with your authentication app...";
    //    public string BackupCodeLabel => _appState.T("QR backup code"); // { get; set; } = "QR backup code";
    //    public string BackupCodeInstructions => _appState.T("Keep secret! Used to deactivate 2FA if you lose your device with the authenticator app."); // { get; set; } = "Keep secret! Used to deactivate 2FA if you lose your device with the authenticator app.";

    //    public string CloseButtonText => _appState.T("Close"); // { get; set; } = "Close";
    //    public string CancelButtonText => _appState.T("Cancel"); // { get; set; } = "Cancel";
    //    public string QrCodeAltText => _appState.T("QR code for setting up 2FA"); // { get; set; } = "QR code for setting up 2FA";
    //    public string Instructions => _appState.T("Note: This code is displayed only once. Ensure you save the backup code in a secure location."); // { get; set; } = "Note: This code is displayed only once. Ensure you save the backup code in a secure location.";

    //    public string AuthenticatorUriLabel => _appState.T("Authenticator URI"); // { get; set; } = "Authenticator URI";
    //    public string CopyUriLabel => _appState.T("Copy URI"); // { get; set; } = "Copy URI";
    //    public string CopyBackupCodeLabel => _appState.T("Copy backup code"); // { get; set; } = "Copy backup code";

    //    public string TitleError => _appState.T("Error"); // { get; set; } = "Error";
    //    public string DBError => _appState.T("Database access not possible (dam is null)."); // { get; set; } = "Database access not possible (dam is null).";
    //}

    // Das Ergebnismodell, das das OnClose-Event zurückgibt
    public class OtpSetupResultModel
    {
        public bool IsAccepted { get; set; }
    }

    //// Bestehendes Modell für die 2FA-Reset-Anfrage
    //public class TwoFactorResetRequestModel
    //{
    //    public string? Backupcode { get; set; }
    //    public string? Emailaccount { get; set; }
    //    public string? Password { get; set; }
    //}

    public class OtpStatus
    {
        public bool ResetRequested { get; set; } = false;
    }

    //public class OtpTextModel
    //{
    //    private readonly Services.AppState.IAppStateBase _appState;

    //    public OtpTextModel(Services.AppState.IAppStateBase appState)
    //    {
    //        _appState = appState;
    //    }

    //    // Texte für die PIN-Eingabe
    //    public string PinLabel => _appState.T("PIN"); // { get; set; } = "PIN";
    //    public string PinDescription => _appState.T("Please enter your PIN."); // { get; set; } = "Please enter your PIN.";
    //    public string PinPlaceholder => _appState.T("Enter your PIN"); // { get; set; } = "Enter your PIN";

    //    // Texte für die OTP-Eingabe
    //    public string OtpLabel => _appState.T("OTP Code"); // { get; set; } = "OTP Code";
    //    public string OtpInputDescription => _appState.T("Please enter all 6 digits."); // { get; set; } = "Please enter all 6 digits.";

    //    // 2FA-Reset-Sektion
    //    public string LostAuthenticatorTitle => _appState.T("Lost Authenticator?"); // { get; set; } = "Lost Authenticator?";
    //    public string ResetDescription => _appState.T("Enter your backup code, account, and password to reset 2FA."); // { get; set; } = "Enter your backup code, account, and password to reset 2FA.";
    //    public string ResetErrorHeader => _appState.T("An error occurred:"); // { get; set; } = "An error occurred:";
    //    public string BackupCodeLabel => _appState.T("Backup Code"); // { get; set; } = "Backup Code";
    //    public string BackupCodeDescription => _appState.T("Your 16-digit backup code."); // { get; set; } = "Your 16-digit backup code.";
    //    public string AccountLabel => _appState.T("Account"); // { get; set; } = "Account";
    //    public string AccountDescription => _appState.T("The account linked to your 2FA."); // { get; set; } = "The account linked to your 2FA.";
    //    public string PasswordLabel => _appState.T("Password"); // { get; set; } = "Password";
    //    public string PasswordDescription => _appState.T("Your account password for verification."); // { get; set; } = "Your account password for verification.";
    //    public string ResetButtonText => _appState.T("Reset 2FA"); // { get; set; } = "Reset 2FA";
    //    public string OtpButtonText => _appState.T("Otp input"); // { get; set; } = "Otp input";
    //    public string PinButtonText => _appState.T("Pin input"); // { get; set; } = "Pin input";
    //    public string ClearButtonText => _appState.T("Clear"); // { get; set; } = "Clear";
    //    public string ResetFormErrorMessage => _appState.T("Please fill in all fields to reset 2FA."); // { get; set; } = "Please fill in all fields to reset 2FA.";
    //    public string IncorrectPin => _appState.T("Pin incorrect, please try again."); // { get; set; } = "Pin incorrect, please try again.";
    //    public string IncorrectOtp => _appState.T("Otp incorrect, please try again."); // { get; set; } = "Otp incorrect, please try again.";

    //    // Dedizierte Properties für Fehlermeldungen
    //    public string PinErrorMessage => _appState.T("Please enter your PIN."); // { get; set; } = "Please enter your PIN.";
    //    public string OtpErrorMessage => _appState.T("Please enter all 6 digits."); // { get; set; } = "Please enter all 6 digits.";

    //    public string ErrorTitle => _appState.T("Error"); // { get; set; } = "Error";
    //    public string ErrorDescription => _appState.T("Error description: "); // { get; set; } = "Error description: ";

    //    public string InfoDescription => _appState.T("Please be patient, the connection to the database (cloud/local) is being secured and this may take a few seconds!"); // { get; set; } = "Please be patient, the connection to the database (cloud/local) is being secured and this may take a few seconds!";

    //    public string LoadingDescription => _appState.T("Loading..."); // { get; set; } = "Loading...";
    //}


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

    //public class OtpRevokeRequestModel
    //{
    //    public string? Account { get; set; }
    //    public string? Password { get; set; }
    //    public string? PasswordRepeat { get; set; }
    //    public string? BackupCode { get; set; }
    //}

    //public class OtpRevokeResultModel
    //{
    //    public bool IsCanceled { get; set; }
    //    public OtpRevokeRequestModel? RequestData { get; set; }
    //}
}

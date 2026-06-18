using System.Diagnostics.CodeAnalysis;

namespace BlazorCore.Services.Authentication
{
    /// <summary>
    /// Container for otp login status
    /// </summary>
    public enum OtpLoginStatus
    {
        user,
        no_user,
        locked,
        otp,
        no_otp,
        unknown
    }

    /// <summary>
    /// Container für externe Identity Provider
    /// </summary>
    public enum IdentityProvider
    {
        Google,
        Microsoft,
        Apple,
        TPC
    }

    /// <summary>
    /// Base class for authentication models.
    /// Used as a root for other authentication models to prevent trimming of base members.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class AuthenticationModelBase
    {
    }

    /// <summary>
    /// Contains the login data.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public class AccountPasswordModel
    {
        public string Account { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PasswordRepeat { get; set; } = string.Empty;
    }

    /// <summary>
    /// Contains the login data.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public class LoginModel : AccountPasswordModel
    {
        public string Language { get; set; } = "EN";
        //public string Account { get; set; } = string.Empty;
        //public string Password { get; set; } = string.Empty;
        public string OtpUserDigitInput { get; set; } = string.Empty;
        public string OtpBackupCode { get; set; } = string.Empty;
        public bool Register { get; set; } = false;
        public bool Termseula { get; set; } = false;
        public IdentityProvider Idp { get; set; } = IdentityProvider.TPC;
    }

    public class ChangePasswordRequestModel : AccountPasswordModel
    {
        //public string Account { get; set; }
        //public string CurrentPassword { get; set; }
        public string NewPassword { get; set; } = string.Empty;
        public string NewPasswordRepeat { get; set; } = string.Empty;
    }

    //public class ErrorTextModel
    //{
    //    private readonly Services.AppState.IAppStateBase _appState;

    //    public ErrorTextModel(Services.AppState.IAppStateBase appState)
    //    {
    //        _appState = appState;
    //    }

    //    public string InfoMessageTitle => _appState.T("Info"); // { get; set; } = "Info";
    //    public string ErrorMessageTitle => _appState.T("Error"); // { get; set; } = "Error";
    //    public string WarningMessageTitle => _appState.T("Warning"); // { get; set; } = "Warning";
    //    public string SuccessMessageTitle => _appState.T("Success"); // { get; set; } = "Success";
    //    public string NoInternetConnection => _appState.T("Registration is not possible because there is no internet connection!"); // { get; set; } = "Registration is not possible because there is no internet connection!";
    //    public string NoCloudUnixTS => _appState.T("No timestamp (required for synchronization) of the account could be found on the cloud. It is therefore not possible to log in with this account."); // { get; set; } = "No timestamp (required for synchronization) of the account could be found on the cloud. It is therefore not possible to log in with this account.";
    //    public string NoLocalAccountCreated => _appState.T("A local account does not exist or cannot be found on the device."); // { get; set; } = "A local account does not exist or cannot be found on the device.";

    //    // ErrorTextModel.cs
    //    public string NoCloudAccountCreated => _appState.T("It could not be verified if a cloud account exists! Local login under this account is therefore not possible."); // { get; set; } = "It could not be verified if a cloud account exists! Local login under this account is therefore not possible.";
    //    public string NoUser => _appState.T("Login is not possible! Username and/or password do not exist."); // { get; set; } = "Login is not possible! Username and/or password do not exist.";
    //    public string NoEntry => _appState.T("User name and/or password entry missing!"); // { get; set; } = "User name and/or password entry missing!";
    //    public string NoLanguage => _appState.T("Language selection is missing!"); // { get; set; } = "Language selection is missing!";
    //    public string NoToken => _appState.T("Registration/login not possible. The server does not provide a token!"); // { get; set; } = "Registration/login not possible. The server does not provide a token!";
    //    public string NoUnixts => _appState.T("Registration/login not possible. The client does not generate a unixts!"); // { get; set; } = "Registration/login not possible. The client does not generate a unixts!";
    //    public string RecordExistsNoAdding => _appState.T("Registration is not possible, the user name already exists."); // { get; set; } = "Registration is not possible, the user name already exists.";
    //    public string NoSynchronizedCategories => _appState.T("Categories could not be synchronized with the cloud, use of the app is not possible!"); // { get; set; } = "Categories could not be synchronized with the cloud, use of the app is not possible!";
    //    public string NoLoggedUser => _appState.T("You are not logged in! Please sign in, then you can use the app."); // { get; set; } = "You are not logged in! Please sign in, then you can use the app.";
    //    public string NoBindingValue => _appState.T("Please select your language!"); // { get; set; } = "Please select your language!";
    //    public string NoUserSettings => _appState.T("User settings could not be loaded, the app cannot be used. Check internet connection, reinstall the app or use web browser. If the problem persists, please contact support!"); // { get; set; } = "User settings could not be loaded, the app cannot be used. Check internet connection, reinstall the app or use web browser. If the problem persists, please contact support!";
    //    public string NoJsonPara => _appState.T("Data could not be sent to web server because JSON data is missing!"); // { get; set; } = "Data could not be sent to web server because JSON data is missing!";
    //}

}
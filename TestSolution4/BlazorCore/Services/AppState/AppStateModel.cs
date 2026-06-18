using p11.UI.Models;
using BlazorCore.Services.GlobalState;
using System.Diagnostics.CodeAnalysis;

namespace BlazorCore.Services.AppState
{
    public class AppStateModelBase
    {
    }

    //public enum APP_PROJECT_TYPE
    //{
    //    WASM = 0,
    //    SERVER = 1,
    //    WPF = 2,
    //    ASPNET = 3,
    //    Unknown = 4
    //}

    public enum APP_LOADING_STATUS
    {
        LOADING = 0,
        FAILED_2FA = 1,
        SPLASHSCREEN = 2,
        READY = 3,
        LOADING_LANGUAGE = 4,
        Unknown = 6
    }

    public enum AppLogLevel
    {
        Log,
        Info,
        Warn,
        Error
    }

    public enum LTR_RTL : int
    {
        LTR,
        RTL,
        Unknown
    }

    public enum STORAGE_LOCATION : int
    {
        CLOUD_LOCAL,
        CLOUD,
        LOCAL,
        Unknown
    }

    public enum SETTINGS_CATEGORIES : int
    {
        GENERAL,
        SAVING,
        ACCESSIBILITY,
        SPECIFIC
    }

    public enum SETTINGS_TYPE : int
    {
        STRING_INPUT,
        STRING_SELECT,
        STRING_COLOR,
        BOOL_CHECKBOX,
        BOOL_SWITCH,
        Unknown
    }


    /// <summary>
    /// Contains all texts for component localization and validation messages.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public class SettingsTextModel
    {
        private readonly Services.AppState.IAppStateBase _appState;

        public SettingsTextModel(Services.AppState.IAppStateBase appState)
        {
            _appState = appState;
        }

        public string LoadingText => _appState.T("Settings are loading..."); // { get; set; } = "Settings are loading...";

        public string Title => _appState.T("Settings"); // { get; set; } = "Settings";

        public string MessageBoxTitle => _appState.T("Exit without saving"); // { get; set; } = "Exit without saving";
        public string MessageBoxMessage => _appState.T("Settings have been changed. Would you like to save them before exiting?"); // { get; set; } = "Settings have been changed. Would you like to save them before exiting?";

        public string SuccessMessageTitle => _appState.T("Success"); // { get; set; } = "Success";
        public string SuccessMessageCloud => _appState.T("Data has been successfully saved to the cloud."); // { get; set; } = "Data has been successfully saved to the cloud.";
        public string SuccessMessageLocal => _appState.T("Data has been successfully saved locally."); // { get; set; } = "Data has been successfully saved locally.";

        public string ErrorMessageTitle => _appState.T("Error"); // { get; set; } = "Error";
        public string ErrorDescription => _appState.T("Error description: "); // { get; set; } = "Error description: ";
        public string ErrorMessage => _appState.T("Password change failed."); // { get; set; } = "Password change failed.";
    }


    public class VersionChangeLog
    {
        public string Date { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ChangeLog { get; set; } = string.Empty;
    }

    public class PinsModel
    {
        public string Pin { get; set; } = string.Empty;
        public string Nonce { get; set; } = Guid.NewGuid().ToString();
        public string EmailHash { get; set; } = string.Empty;
    }

    public class PepperClientInfo()
    {
        public string? AccountHash;
        public byte[]? ApplicationNamePepper;
        public string? PasHash;
        public string? ApplicationName;
    }

    public class SettingMetadata
    {
        public string ParameterName { get; set; } = string.Empty;
        public string ParameterStringValue { get; set; } = string.Empty;
        public bool ParameterBoolValue { get; set; } = false;
        public string ParameterTemplateValue { get; set; } = string.Empty;
        public string ParameterType { get; set; } = string.Empty;
        public string ParameterHtmlElement { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class SettingMetadataModel : ICloneable
    {
        private string _valStr = string.Empty;
        private bool _valBool = false;

        public string ParaName { get; set; } = string.Empty;

        public string ValStr
        {
            get => _valStr;
            set
            {
                if (_valStr != value)
                {
                    _valStr = value;
                    Changed = true;
                }
            }
        }

        public bool ValBool
        {
            get => _valBool;
            set
            {
                if (_valBool != value)
                {
                    _valBool = value;
                    Changed = true;
                }
            }
        }

        public string ValTempl { get; set; } = string.Empty;
        public SETTINGS_TYPE ParaType { get; set; } = SETTINGS_TYPE.Unknown;
        public string Label { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public SETTINGS_CATEGORIES Category { get; set; } = SETTINGS_CATEGORIES.GENERAL;

        // Neues Flag für Änderungen
        public bool Changed { get; private set; } = false;

        public void ResetChanged() => Changed = false;


        public object Clone()
        {
            var copy = (SettingMetadataModel)this.MemberwiseClone();
            copy.ResetChanged();
            return copy;
            //return this.MemberwiseClone();
        }
    }

    public class TodoCountModel
    {
        public int Top { get; set; }          // Anzahl tatsächlich angezeigter Datensätze
        public int CountFilter { get; set; }  // Anzahl nach Filter
        public int CountAll { get; set; }     // Gesamtanzahl aller Datensätze
    }



    //public class AppConfig
    //{
    //    // General
    //    public string ApplicationName { get; init; } = string.Empty;
    //    public string ApplicationDescription { get; init; } = string.Empty;
    //    public string DefaultLanguage { get; init; } = string.Empty;
    //    public string AppVersion { get; init; } = string.Empty;
    //    public string Company { get; init; } = string.Empty;
    //    public string TableSchema { get; init; } = string.Empty;

    //    // Hosting
    //    public string ApplicationDomain { get; init; } = string.Empty;
    //    public string ApplicationUrl { get; init; } = string.Empty;
    //    //public string ApplicationUrlLocal { get; init; } = string.Empty;
    //    public string ApplicationApiUrl { get; init; } = string.Empty;

    //    // Project
    //    public string Aumid { get; init; } = string.Empty;
    //    public string CLSID { get; init; } = string.Empty;
    //    public int EpochYear { get; init; } = 2026;
    //    public string WindowsBorderBckGrColor { get; init; } = string.Empty;
    //    public string WindowsBorderForeColor { get; init; } = string.Empty;

    //    //public const bool NeedAuthentication = true; // Wenn true, dann können Anonyme-Benutzer generell auch zugelassen werden
    //    //public const string MobileSafeArea = "#333333"; // Wird plattformspezifisch in MainPage.xaml.cs und Android/MainActivity.cs => OnCreate() definiert

    //    // Other
    //    public string ErrorText { get; init; } = string.Empty;
    //    public int TaskDelay_Capacitor { get; init; } = 50;
    //    public bool IsDebugEnabled { get; init; } = false;

    //    // Connection
    //    public string ConnectionsServerFolder { get; init; } = string.Empty;
    //    public string SecurityConfigJsonFilename { get; init; } = string.Empty;
    //    public string PepperApp { get; init; } = string.Empty;
    //    public string PepperAppWasm { get; init; } = string.Empty;
    //}

    //public class AppConfig
    //{
    //    public Init? Init { get; init; }
    //}

    //public class LocalStorageModel
    //{
    //    public string storagelocation { get; init; } = string.Empty;
    //    public string oauth_token { get; init; } = string.Empty;
    //    public string language { get; init; } = string.Empty;
    //    public string ltrrtl { get; init; } = string.Empty;
    //    public string fontfamily { get; init; } = string.Empty;
    //    public string fontsize { get; init; } = string.Empty;
    //    public string fontweigh { get; init; } = string.Empty;
    //    public string fontspacing { get; init; } = string.Empty;
    //    public string fontlineheight { get; init; } = string.Empty;
    //    public string thememode { get; init; } = string.Empty;
    //    public string sqlitekey { get; init; } = string.Empty;

    //    public string last_failed_reset2fa { get; init; } = string.Empty;
    //    public string last_failed_login { get; init; } = string.Empty;
    //    public string accessibility { get; init; } = string.Empty;
    //    public string accessibilitylandingpage { get; init; } = string.Empty;
    //    public string accessibilitysmartview { get; init; } = string.Empty;
    //    public string idp { get; init; } = string.Empty;
    //    public string design { get; init; } = string.Empty;
    //    //public readonly string all = "all__";
    //    public string Unknown { get; init; } = string.Empty;

    //    public string pin { get; init; } = string.Empty;

    //    public string storeurls { get; init; } = string.Empty;
    //}

    //public class DownloadAppModel
    //{
    //    public string? Type { get; set; }
    //    public string? Id { get; set; }
    //    public string? Icon { get; set; }
    //    public string? Title { get; set; }
    //    public string? Url { get; set; }
    //}

    //public class Sections
    //{
    //    public List<string>? TablesMSSQL { get; init; }

    //    public List<SqlClient.TableDefinition>? ParaMSSQL { get; init; }

    //    public Dictionary<string, string>? Languages { get; init; }

    //    public List<FontFamilyModel>? Fonts { get; init; }

    //    public List<DownloadAppModel>? DownloadApp { get; set; }

    //    public LocalStorageModel? LocalStorage { get; init; }
    //}

    //public class UserParameterModel
    //{
    //    // General
    //    public string DebugId  { get; init; } = string.Empty;
    //    public string StorageMode { get; init; } = string.Empty;
    //    public string Top { get; init; } = string.Empty;

    //    // Your other project parameter
    //}

    //public class UserParameterHideModel
    //{
    //    public string OtpBackupCode { get; init; } = string.Empty;
    //}

    //public class Dataset
    //{
    //    public List<SettingMetadataModel>? Settings { get; init; }

    //    public List<VersionChangeLog>? VersionChangeLog { get; init; }

    //    public UserParameterModel? UserParameter { get; init; }
    //    public UserParameterHideModel? UserParameterHide { get; init; }
    //}

}

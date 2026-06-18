using p11.UI.Models;
using BlazorCore.Services.GlobalState;
using System.Diagnostics.CodeAnalysis;

namespace BlazorCore.Services.AppState
{
    public class AppStateModelBase
    {
    }

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
        

}

using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.SqlClient;
using p11.UI.Models;

namespace TestSolution4.Shared.Global
{
    public static class Configuration
    {
        public static ConfigurationGeneral ConfigGeneral { get; } = new ConfigurationGeneral
        {
            // General
            ApplicationName = "TestSolution4",
            ApplicationDescription = "TestSolution4 is your versatile app...",
            DefaultLanguage = "EN",
            AllSupportedLanguageCodes = "EN,DE,SRL,SRC,FR,IT,AR,ZH,HI,ES,ID,PT",
            AppVersion = "1.0.03",
            Company = "true-perfect-code",
            TableSchema = "dbuser_TestSolution4",
            FileExtensionJson = ".json",
            StorageLocation = STORAGE_LOCATION.LOCAL,
            LocalStorageType = LOCAL_STORAGE_TYPE.MEMORY,
            LocalStorageEncrypt = false,

            // Hosting
            ApplicationDomain = "testsolution4.com",
            //ApplicationUrlLocal = "https://localhost:7262",
            ApplicationUrl = "https://testsolution4.com",
            ApplicationApiUrl = "https://testsolution4.true-perfect-code.ch",

            // Project
            Aumid = "TestSolution4.Wpf.Desktop.Local",
            CLSID = "{XXXXXXXXXX-XXXX-XXXX-XXXXX-XXXXXXXXXXXXX}",
            EpochYear = 2026,
            WindowsBorderBckGrColor = "#e0e0e0",
            WindowsBorderForeColor = "#333",

            // Other
            TaskDelay_Capacitor = 50,
            IsDebugEnabled = false,
            EncryptDecryptWebApi = false,

            // Connection
            ConnectionsServerFolder = "_Connections",
            SecurityConfigJsonFilename = "testsolution4.security.config.json",
            PepperApp = "", // "Bn9XQV4dlPebH1i4BE+5WhVYOHGHhbhUjiKg64YbtpHVK7VXT+jgD5dBPPQhMSxgUAXWEIRMxgqu2l+K",
            PepperAppWasm = "", // "JISgRxiNwS7+tJVnuoMpM07P+9Z7RWhJbPnQWveghLAdD3jUQvDUfHVjwiIa/KJIRsamTQ2odVXLUGn63GOF5Q==",

            // Error handling
            ErrorText = "We’re sorry, but an error occurred!",
            WebapiExceptionError = "exception_error",
            ShowConnectionStringByError = false,
        };

        private static class EndpointPaths
        {
            internal const string GetTokenDataTPCuser = "/security/tokentpc";
            internal const string GetTokenDataIDPuser = "/security/tokenidp";
            internal const string ChangePassword = "/security/changepassword";
            internal const string GetScalar = "/api/spscalar";
            internal const string SetData = "/api/spnonquery";
            internal const string GetRows = "/api/spreader";
            internal const string ExcelLang = "/api/excellang";
            internal const string Feedback = "/api/feedback";
            internal const string Anonymous = "/api/anonymous";
            internal const string Authorizedconnection = "/api/authorizedconnection";
            internal const string CheckToken = "/api/check";
            internal const string AuthHub = "/authHub";
        }

        // Jetzt kannst du sauber bauen, ohne Wiederholungen!
        public static ConfigurationWebapi ConfigWebapi { get; } = new ConfigurationWebapi
        {
            // Endpoints - NUR ZUWEISUNG, keine magischen Strings direkt!
            endpoint_GetTokenDataTPCuser = EndpointPaths.GetTokenDataTPCuser,
            endpoint_GetTokenDataIDPuser = EndpointPaths.GetTokenDataIDPuser,
            endpoint_ChangePassword = EndpointPaths.ChangePassword,
            endpoint_GetScalar = EndpointPaths.GetScalar,
            endpoint_SetData = EndpointPaths.SetData,
            endpoint_GetRows = EndpointPaths.GetRows,
            endpoint_ExcelLang = EndpointPaths.ExcelLang,
            endpoint_Feedback = EndpointPaths.Feedback,
            endpoint_Anonymous = EndpointPaths.Anonymous,
            endpoint_Authorizedconnection = EndpointPaths.Authorizedconnection,
            endpoint_CheckToken = EndpointPaths.CheckToken,
            endpoint_AuthHub = EndpointPaths.AuthHub,

            // URLs - basierend auf DEN SELBEN Konstanten!
            url_GetTokenDataTPCuser = $"{ConfigGeneral.ApplicationApiUrl}{EndpointPaths.GetTokenDataTPCuser}",
            url_GetTokenDataIDPuser = $"{ConfigGeneral.ApplicationApiUrl}{EndpointPaths.GetTokenDataIDPuser}",
            url_ChangePassword = $"{ConfigGeneral.ApplicationApiUrl}{EndpointPaths.ChangePassword}",
            url_GetScalar = $"{ConfigGeneral.ApplicationApiUrl}{EndpointPaths.GetScalar}",
            url_SetData = $"{ConfigGeneral.ApplicationApiUrl}{EndpointPaths.SetData}",
            url_GetRows = $"{ConfigGeneral.ApplicationApiUrl}{EndpointPaths.GetRows}",
            url_ExcelLang = $"{ConfigGeneral.ApplicationApiUrl}{EndpointPaths.ExcelLang}",
            url_Feedback = $"{ConfigGeneral.ApplicationApiUrl}{EndpointPaths.Feedback}",
            url_Anonymous = $"{ConfigGeneral.ApplicationApiUrl}{EndpointPaths.Anonymous}",
            url_Authorizedconnection = $"{ConfigGeneral.ApplicationApiUrl}{EndpointPaths.Authorizedconnection}",
            url_CheckToken = $"{ConfigGeneral.ApplicationApiUrl}{EndpointPaths.CheckToken}",
            url_AuthHub = $"{ConfigGeneral.ApplicationApiUrl}{EndpointPaths.AuthHub}"
        };

    }

    public static class Catalog
    {
        public static Sections Sections { get; set; } = new Sections
        {
            TablesMSSQL = new List<string>
            {
                "AuthUsers",
                "AuthUsersExtend",
                "AppParameter",
                "SharingUsers"
            },

            ParaMSSQL = new List<TableDefinition>
            {
                //////////////
                // ACHTUNG !!!
                //////////////
                //
                // Bei jeder anpassung hier muss auch WebApi Projekt neu kompiliert und publiziert werden, sonst funktioniert MAUI Blazor App nicht !!!
                //
                //////////////
                // ACHTUNG !!!
                //////////////
                new TableDefinition() { SP_PARAMETER_NAME = "@Json", COLUMN_NAME = "@Json", DATA_TYPE = "NVarChar", COL_SIZE = -1 },
                new TableDefinition() { SP_PARAMETER_NAME = "@imgJpeg", COLUMN_NAME = "@imgJpeg", DATA_TYPE = "NVarChar", COL_SIZE = -1 },
                new TableDefinition() { SP_PARAMETER_NAME = "@imgJpegThumbnail", COLUMN_NAME = "@imgJpegThumbnail", DATA_TYPE = "NVarChar", COL_SIZE = -1 },
                new TableDefinition() { SP_PARAMETER_NAME = "@Passphrase", COLUMN_NAME = "@Passphrase", DATA_TYPE = "NVarChar", COL_SIZE = 4000 },

                new TableDefinition() { SP_PARAMETER_NAME = "@ID", COLUMN_NAME = "@ID", DATA_TYPE = "INT", COL_SIZE = 0 },
                new TableDefinition() { SP_PARAMETER_NAME = "@UnixTS", COLUMN_NAME = "@UnixTS", DATA_TYPE = "VarChar", COL_SIZE = 35 },
                new TableDefinition() { SP_PARAMETER_NAME = "@UnixTS2", COLUMN_NAME = "@UnixTS2", DATA_TYPE = "VarChar", COL_SIZE = 35 },
                //new TableDefinition() { SP_PARAMETER_NAME = "@AuthUsers_ID", COLUMN_NAME = "@AuthUsers_ID", DATA_TYPE = "INT", COL_SIZE = 0 },
                new TableDefinition() { SP_PARAMETER_NAME = "@AuthUsers_UnixTS", COLUMN_NAME = "@AuthUsers_UnixTS", DATA_TYPE = "VarChar", COL_SIZE = 35 },
                new TableDefinition() { SP_PARAMETER_NAME = "@DisplayName", COLUMN_NAME = "@DisplayName", DATA_TYPE = "NVarChar", COL_SIZE = 256 },
                new TableDefinition() { SP_PARAMETER_NAME = "@Details", COLUMN_NAME = "@Details", DATA_TYPE = "NVarChar", COL_SIZE = 2048 },
                new TableDefinition() { SP_PARAMETER_NAME = "@RecordDate", COLUMN_NAME = "@RecordDate", DATA_TYPE = "Date", COL_SIZE = 0 },
                new TableDefinition() { SP_PARAMETER_NAME = "@RecordDateTimeUnix", COLUMN_NAME = "@RecordDateTimeUnix", DATA_TYPE = "BIGINT", COL_SIZE = 0 },
                new TableDefinition() { SP_PARAMETER_NAME = "@sorter", COLUMN_NAME = "@sorter", DATA_TYPE = "INT", COL_SIZE = 0 },
                new TableDefinition() { SP_PARAMETER_NAME = "@LastUpdateUnixTS", COLUMN_NAME = "@LastUpdateUnixTS", DATA_TYPE = "BIGINT", COL_SIZE = 0 },
                new TableDefinition() { SP_PARAMETER_NAME = "@LastUpdateUnixTS2", COLUMN_NAME = "@LastUpdateUnixTS2", DATA_TYPE = "BIGINT", COL_SIZE = 0 },

                new TableDefinition() { SP_PARAMETER_NAME = "@EmailHash", COLUMN_NAME = "@EmailHash", DATA_TYPE = "NVarChar", COL_SIZE = 256 },
                new TableDefinition() { SP_PARAMETER_NAME = "@PasswordHash", COLUMN_NAME = "@PasswordHash", DATA_TYPE = "NVarChar", COL_SIZE = 256 },
                new TableDefinition() { SP_PARAMETER_NAME = "@PasswordHashNew", COLUMN_NAME = "@PasswordHashNew", DATA_TYPE = "NVarChar", COL_SIZE = 256 },

                new TableDefinition() { SP_PARAMETER_NAME = "@TOP", COLUMN_NAME = "@TOP", DATA_TYPE = "INT", COL_SIZE = 0 },
                new TableDefinition() { SP_PARAMETER_NAME = "@OrderBy", COLUMN_NAME = "@OrderBy", DATA_TYPE = "NVarChar", COL_SIZE = 256 },
                new TableDefinition() { SP_PARAMETER_NAME = "@SearchFields", COLUMN_NAME = "@SearchFields", DATA_TYPE = "NVarChar", COL_SIZE = 512 },
                new TableDefinition() { SP_PARAMETER_NAME = "@TableName", COLUMN_NAME = "@TableName", DATA_TYPE = "NVarChar", COL_SIZE = 128 },

                new TableDefinition() { SP_PARAMETER_NAME = "@SharingUsersUnixTS", COLUMN_NAME = "@SharingUsersUnixTS", DATA_TYPE = "VarChar", COL_SIZE = -1 },
                new TableDefinition() { SP_PARAMETER_NAME = "@OtpBackupCode", COLUMN_NAME = "@OtpBackupCode", DATA_TYPE = "NVarChar", COL_SIZE = 512 },
                new TableDefinition() { SP_PARAMETER_NAME = "@IsMigration", COLUMN_NAME = "@IsMigration", DATA_TYPE = "BIT", COL_SIZE = 0 },
            },

            Languages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"EN", "English"},
                {"DE", "Deutsch"},
                {"FR", "Français"},
                {"IT", "Italiano"},
                {"AR", "العربية"},
                {"ZH", "中文"},
                {"HI", "हिन्दी"},
                {"ES", "Español"},
                {"ID", "Indonesia"},
                {"PT", "Brasileiro"},
                {"SRL", "Srpski (Latin)"},
                {"SRC", "Српски (Cyrillic)"}
            },

            Fonts = new List<FontFamilyModel>
            {
                new FontFamilyModel { Font = "Default font", CssFontFamily = "Comfortaa" },
                new FontFamilyModel { Font = "Cinzel", CssFontFamily = "Cinzel" },
                new FontFamilyModel { Font = "Dyslexic font", CssFontFamily = "OpenDyslexicAlta" },
                new FontFamilyModel { Font = "Atkinson font", CssFontFamily = "Atkinson Hyperlegible" },
                new FontFamilyModel { Font = "Lexend font", CssFontFamily = "Lexend" },
                new FontFamilyModel { Font = "القرآن الأميري", CssFontFamily = "Amiri Quran" },
                new FontFamilyModel { Font = "خط القاهرة", CssFontFamily = "Cairo" },
                new FontFamilyModel { Font = "خط Tajawal", CssFontFamily = "Tajawal" },
            },

            DownloadApp = new List<DownloadAppModel>
            {
                new DownloadAppModel { Icon = "bi bi-windows", Id = "StoreUrl_Microsoft", Type = "store", Title = "Microsoft Store", Url = "https://apps.microsoft.com/search/publisher?name=True%20Perfect%20Code" },
                new DownloadAppModel { Icon = "bi bi-android", Id = "StoreUrl_Google", Type = "store", Title = "Google Play", Url = "https://play.google.com/store/apps/developer?id=True+Perfect+Code" },
                new DownloadAppModel { Icon = "bi bi-phone", Id = "StoreUrl_ApplePhone", Type = "store", Title = "App Store", Url = "https://apps.apple.com/us/developer/daniel-simic/id1733470934" },
                new DownloadAppModel { Icon = "bi bi-apple", Id = "StoreUrl_AppleMac", Type = "store", Title = "Mac App Store", Url = "https://apps.apple.com/us/developer/daniel-simic/id1733470934" },
                new DownloadAppModel { Icon = "bi bi-globe2", Id = "StoreUrl_Web", Type = "web", Title = "Open in browser", Url = "https://pmunus.de/" },
                new DownloadAppModel { Icon = "bi bi-tux", Id = "StoreUrl_Pwa", Type = "web", Title = "Linux PWA Desktop", Url = "https://pwa.pmunus.de" },
                new DownloadAppModel { Icon = "bi bi-filetype-exe", Id = "StoreUrl_Exe", Type = "desktop", Title = "Windows EXE (portable)", Url = "https://portable.pmunus.de" },
            },

            LocalStorage = new LocalStorageModel
            {
                storagelocation = $"storagelocation__{Configuration.ConfigGeneral.ApplicationName}",
                oauth_token = $"oauth_token__{Configuration.ConfigGeneral.ApplicationName}",
                language = $"language__{Configuration.ConfigGeneral.ApplicationName}",
                ltrrtl = $"ltrrtl__{Configuration.ConfigGeneral.ApplicationName}",
                fontfamily = $"fontfamily__{Configuration.ConfigGeneral.ApplicationName}",
                fontsize = $"fontsize__{Configuration.ConfigGeneral.ApplicationName}",
                fontweigh = $"fontweigh__{Configuration.ConfigGeneral.ApplicationName}",
                fontspacing = $"fontspacing__{Configuration.ConfigGeneral.ApplicationName}",
                fontlineheight = $"fontlineheight__{Configuration.ConfigGeneral.ApplicationName}",
                thememode = $"thememode__{Configuration.ConfigGeneral.ApplicationName}",
                sqlitekey = $"sqlitekey__{Configuration.ConfigGeneral.ApplicationName}",

                last_failed_reset2fa = $"last_failed_reset2fa__{Configuration.ConfigGeneral.ApplicationName}",
                last_failed_login = $"last_failed_login__{Configuration.ConfigGeneral.ApplicationName}",
                accessibility = $"accessibility__{Configuration.ConfigGeneral.ApplicationName}",
                accessibilitylandingpage = $"accessibilitylandingpage__{Configuration.ConfigGeneral.ApplicationName}",
                accessibilitysmartview = $"accessibilitysmartview__{Configuration.ConfigGeneral.ApplicationName}",
                idp = $"idp__{Configuration.ConfigGeneral.ApplicationName}",
                design = $"design__{Configuration.ConfigGeneral.ApplicationName}",
                //public readonly string all = "all__",
                Unknown = $"Unknown",

                pin = $"pin__{Configuration.ConfigGeneral.ApplicationName}",

                storeurls = $"storeurls__{Configuration.ConfigGeneral.ApplicationName}"
            }
        };
    }

    public static class Data
    {
        public static Dataset Dataset { get; }

        static Data()
        {
            // 1. Projektspezifische Werte definieren
            var userParameter = new UserParameterModel
            {
                DebugId = "DebugId",
                StorageMode = "StorageMode",
                Top = "Top"
            };

            var userParameterHide = new UserParameterHideModel
            {
                OtpBackupCode = "OtpBackupCode"
            };

            // 2. Settings darauf basierend aufbauen
            var settings = new List<SettingMetadataModel>
            {
                new SettingMetadataModel
                {
                    ParaName = userParameter.DebugId,
                    ParaType = SETTINGS_TYPE.STRING_INPUT,
                    Label = "Debug-ID",
                    Desc = "Set debug ID to bypass your anonymity (e.g., for support).",
                    Category = SETTINGS_CATEGORIES.GENERAL
                },
                new SettingMetadataModel
                {
                    ParaName = userParameter.StorageMode,
                    ValStr = "0",
                    ValTempl = "0,Cloud/Local;1,Cloud;2,Local",
                    ParaType = SETTINGS_TYPE.STRING_SELECT,
                    Label = "Select storage location",
                    Desc = "Select where you want your data to be stored (CLOUD_LOCAL=cloud and local, CLOUD=cloud only, and LOCAL=local only).",
                    Category = SETTINGS_CATEGORIES.SAVING
                },
                new SettingMetadataModel
                {
                    ParaName = userParameter.Top,
                    ValStr = "100",
                    ValTempl = "50,50;100,100;500,500",
                    ParaType = SETTINGS_TYPE.STRING_SELECT,
                    Label = "Number of entries displayed",
                    Desc = "Select here how many entries should be displayed in the list.",
                    Category = SETTINGS_CATEGORIES.ACCESSIBILITY
                }
            };

            // 3. VersionChangeLog
            var versionChangeLog = new List<VersionChangeLog>
            {
                new VersionChangeLog { Date = "07.03.2026", Version = "1.0.03", ChangeLog = "Performance improvements:" },
                new VersionChangeLog { Date = "", Version = "", ChangeLog = "- Optimization data sync." },

                new VersionChangeLog { Date = "15.11.2025", Version = "1.0.02", ChangeLog = "Bug fixes and performance improvements:" },
                new VersionChangeLog { Date = "", Version = "", ChangeLog = "- Optimization services." },
                new VersionChangeLog { Date = "", Version = "", ChangeLog = "- Fix sharing." },
                new VersionChangeLog { Date = "", Version = "", ChangeLog = "- Fix export/import." },

                new VersionChangeLog { Date = "04.09.2025", Version = "1.0.01", ChangeLog = "Bug fixes and performance improvements:" },
                new VersionChangeLog { Date = "", Version = "", ChangeLog = "- Fix saving the image." },
                new VersionChangeLog { Date = "", Version = "", ChangeLog = "- Fix task display." }
            };

            // 4. Finales Objekt bauen
            Dataset = new Dataset
            {
                UserParameter = userParameter,
                UserParameterHide = userParameterHide,
                Settings = settings,
                VersionChangeLog = versionChangeLog
            };
        }
    }

    
}

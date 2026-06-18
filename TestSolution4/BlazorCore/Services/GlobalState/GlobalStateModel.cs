using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using Microsoft.JSInterop;
using p11.UI.Models;
using System.Text.Json.Serialization;

namespace BlazorCore.Services.GlobalState
{
    public class GlobalStateModelBase
    {
    }


    public class ConfigurationGeneral
    {
        // General 
        public string ApplicationName { get; init; } = string.Empty;
        public string ApplicationDescription { get; init; } = string.Empty;
        public string DefaultLanguage { get; init; } = string.Empty;
        public string AllSupportedLanguageCodes { get; init; } = string.Empty;
        public string AppVersion { get; init; } = string.Empty;
        public string Company { get; init; } = string.Empty;
        public string TableSchema { get; init; } = string.Empty;
        public string FileExtensionJson { get; init; } = string.Empty;
        public STORAGE_LOCATION StorageLocation { get; init; } = STORAGE_LOCATION.CLOUD;
        public LOCAL_STORAGE_TYPE LocalStorageType { get; set; } = LOCAL_STORAGE_TYPE.JSON_HYBRID;
        public bool LocalStorageEncrypt { get; init; } = false;

        // Hosting
        public string ApplicationDomain { get; init; } = string.Empty;
        public string ApplicationUrl { get; init; } = string.Empty;
        //public string ApplicationUrlLocal { get; init; } = string.Empty;
        public string ApplicationApiUrl { get; init; } = string.Empty;

        // Project
        public string Aumid { get; init; } = string.Empty;
        public string CLSID { get; init; } = string.Empty;
        public int EpochYear { get; init; } = 2026;
        public string WindowsBorderBckGrColor { get; init; } = string.Empty;
        public string WindowsBorderForeColor { get; init; } = string.Empty;

        //public const bool NeedAuthentication = true; // Wenn true, dann können Anonyme-Benutzer generell auch zugelassen werden
        //public const string MobileSafeArea = "#333333"; // Wird plattformspezifisch in MainPage.xaml.cs und Android/MainActivity.cs => OnCreate() definiert

        // Other
        public int TaskDelay_Capacitor { get; init; } = 50;
        public bool IsDebugEnabled { get; init; } = false;
        public bool EncryptDecryptWebApi { get; init; } = false;

        // Connection
        public string ConnectionsServerFolder { get; init; } = string.Empty;
        public string SecurityConfigJsonFilename { get; init; } = string.Empty;
        public string PepperApp { get; init; } = string.Empty;
        public string PepperAppWasm { get; init; } = string.Empty;

        // Error handling
        public string ErrorText { get; init; } = string.Empty;
        public string WebapiExceptionError { get; init; } = string.Empty;
        public bool ShowConnectionStringByError { get; init; } = false;
    }



    public class ConfigurationWebapi //TpcWebApiEndpointsModel
    {
        public string url_GetTokenDataTPCuser { get; init; } = string.Empty;
        public string url_GetTokenDataIDPuser { get; init; } = string.Empty;
        public string url_ChangePassword { get; init; } = string.Empty;
        public string url_GetScalar { get; init; } = string.Empty;
        public string url_SetData { get; init; } = string.Empty;
        public string url_GetRows { get; init; } = string.Empty;
        public string url_ExcelLang { get; init; } = string.Empty;
        public string url_Feedback { get; init; } = string.Empty;
        public string url_Anonymous { get; init; } = string.Empty;
        public string url_Authorizedconnection { get; init; } = string.Empty;
        public string url_CheckToken { get; init; } = string.Empty;
        public string url_AuthHub { get; init; } = string.Empty;

        public string endpoint_GetTokenDataTPCuser { get; init; } = string.Empty;
        public string endpoint_GetTokenDataIDPuser { get; init; } = string.Empty;
        public string endpoint_ChangePassword { get; init; } = string.Empty;
        public string endpoint_GetScalar { get; init; } = string.Empty;
        public string endpoint_SetData { get; init; } = string.Empty;
        public string endpoint_GetRows { get; init; } = string.Empty;
        public string endpoint_ExcelLang { get; init; } = string.Empty;
        public string endpoint_Feedback { get; init; } = string.Empty;
        public string endpoint_Anonymous { get; init; } = string.Empty;
        public string endpoint_Authorizedconnection { get; init; } = string.Empty;
        public string endpoint_CheckToken { get; init; } = string.Empty;
        public string endpoint_AuthHub { get; init; } = string.Empty;
    }






    public class LocalStorageModel
    {
        public string storagelocation { get; init; } = string.Empty;
        public string oauth_token { get; init; } = string.Empty;
        public string language { get; init; } = string.Empty;
        public string ltrrtl { get; init; } = string.Empty;
        public string fontfamily { get; init; } = string.Empty;
        public string fontsize { get; init; } = string.Empty;
        public string fontweigh { get; init; } = string.Empty;
        public string fontspacing { get; init; } = string.Empty;
        public string fontlineheight { get; init; } = string.Empty;
        public string thememode { get; init; } = string.Empty;
        public string sqlitekey { get; init; } = string.Empty;

        public string last_failed_reset2fa { get; init; } = string.Empty;
        public string last_failed_login { get; init; } = string.Empty;
        public string accessibility { get; init; } = string.Empty;
        public string accessibilitylandingpage { get; init; } = string.Empty;
        public string accessibilitysmartview { get; init; } = string.Empty;
        public string idp { get; init; } = string.Empty;
        public string design { get; init; } = string.Empty;
        //public readonly string all = "all__";
        public string Unknown { get; init; } = string.Empty;

        public string pin { get; init; } = string.Empty;

        public string storeurls { get; init; } = string.Empty;
    }

    public class DownloadAppModel
    {
        public string? Type { get; set; }
        public string? Id { get; set; }
        public string? Icon { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
    }

    public class Sections
    {
        public List<string>? TablesMSSQL { get; set; }

        public List<SqlClient.TableDefinition>? ParaMSSQL { get; set; }

        public Dictionary<string, string>? Languages { get; init; }

        public List<FontFamilyModel>? Fonts { get; init; }

        public List<DownloadAppModel>? DownloadApp { get; set; }

        public LocalStorageModel? LocalStorage { get; init; }
    }

    public class UserParameterModel
    {
        // General
        public string DebugId { get; init; } = string.Empty;
        public string StorageMode { get; init; } = string.Empty;
        public string Top { get; init; } = string.Empty;

        // Your other project parameter
    }

    public class UserParameterHideModel
    {
        public string OtpBackupCode { get; init; } = string.Empty;
    }

    public class Dataset
    {
        public List<SettingMetadataModel>? Settings { get; init; }

        public List<VersionChangeLog>? VersionChangeLog { get; init; }

        public UserParameterModel? UserParameter { get; init; }
        public UserParameterHideModel? UserParameterHide { get; init; }
    }













    // Json
    /// <summary>
    /// Datentypdefinitionen der ConnectionString Parameter im der Connection-Datei
    /// </summary>
    public class ConnectionStringParametersModel
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Server { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Database { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? AuthUsers_ID { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Password { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Encrypt { get; set; } = false;

        public bool Integrated_Security { get; set; } = false;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Connection_Timeout { get; set; } = 0;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Pooling { get; set; } = true;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Min_Pool_Size { get; set; } = 0;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Max_Pool_Size { get; set; } = 0;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool MultipleActiveResultSets { get; set; } = false;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Application_Name { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool TrustServerCertificate { get; set; } = false;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Current_Language { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Packet_Size { get; set; } = 0;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Workstation_ID { get; set; }

        [JsonIgnore]
        public bool Int__ShowForm { get; set; } = false;

        [JsonIgnore]
        public string Int__ConnectionFileName { get; set; } = "";

        [JsonIgnore]
        public string Int__Pathtype { get; set; } = "";

        [JsonIgnore]
        public string Int__ConnectionStringName { get; set; } = "";

        [JsonIgnore]
        public string Int__PasswordDecrypted { get; set; } = "";

        //[JsonIgnore]
        //public List<SpracheModel> Int_list_sprache { get; set; } = new();


        public void Clear()
        {
            Server = "";
            Database = "";
            AuthUsers_ID = "";
            Password = "";
            Encrypt = false;
            Integrated_Security = false;
            Connection_Timeout = 0;
            Pooling = true;
            Min_Pool_Size = 0;
            Max_Pool_Size = 0;
            MultipleActiveResultSets = false;
            Application_Name = "";
            TrustServerCertificate = false;
            Current_Language = "";
            Packet_Size = 0;
            Workstation_ID = "";

            Int__ConnectionFileName = "";
            Int__ConnectionStringName = "";
            Int__PasswordDecrypted = "";
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    // Definition, die dem JSON-Array-Eintrag entspricht
    public class TranslationEntryModel
    {
        // Die Eigenschaftsnamen müssen exakt den Keys in der JSON-Datei entsprechen
        // (AOT-Source Generation bevorzugt genaue Übereinstimmungen, auch wenn CaseInsensitive=true gesetzt ist)

        public string EN { get; set; } = string.Empty;
        public string DE { get; set; } = string.Empty;
        public string SRL { get; set; } = string.Empty; // Serbisch (Latein)
        public string SRC { get; set; } = string.Empty; // Serbisch (Kyrillisch)
        public string FR { get; set; } = string.Empty;
        public string IT { get; set; } = string.Empty;
        public string AR { get; set; } = string.Empty;
        public string ZH { get; set; } = string.Empty;
        public string HI { get; set; } = string.Empty;
        public string ES { get; set; } = string.Empty;
        public string ID { get; set; } = string.Empty;
        public string PT { get; set; } = string.Empty;
    }


    // --- NEUE HINZUFÜGUNG: Die performante Struktur ---
    /// <summary>
    /// Die zentrale Datenstruktur für alle geladenen Übersetzungen
    /// über alle Sprachen hinweg.
    /// </summary>
    //public class Translations
    //{
    //    // 1. Die Haupt-Datenstruktur:
    //    // [Sprach-Code (z.B. "DE")] -> [Englischer Text (Key)] -> [Übersetzung]
    //    public Dictionary<string, Dictionary<string, string>> LanguageMaps { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    //    // 2. Fallback-Sprache
    //    private const string FallbackLanguage = "EN";

    //    // 3. Die Performante Abfrage-Methode (für den späteren AppState-Load)
    //    /// <summary>
    //    /// Gibt das gesamte Dictionary für eine bestimmte Sprache zurück.
    //    /// </summary>
    //    public Dictionary<string, string>? GetLanguageMap(string languageCode)
    //    {
    //        if (LanguageMaps.TryGetValue(languageCode, out var translationDictionary))
    //        {
    //            return translationDictionary;
    //        }
    //        return null;
    //    }

    //    // Du könntest hier auch die Get(key) Methode definieren, 
    //    // falls der AppState direkt aus dem GlobalState lesen soll,
    //    // aber das würde deinen aktuellen AppState-Workflow ändern.
    //}

    public class Translations
    {
        // INTERNE Datenstruktur für maximale Performance
        private readonly Dictionary<string, string>[] _languageMaps;
        private readonly Dictionary<string, int> _languageIndex = new(StringComparer.OrdinalIgnoreCase);

        // Property für Kompatibilität (falls externer Code darauf zugreift)
        public Dictionary<string, Dictionary<string, string>> LanguageMaps
        {
            get
            {
                var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in _languageIndex)
                {
                    result[kvp.Key] = _languageMaps[kvp.Value];
                }
                return result;
            }
        }

        public Translations()
        {
            //Console.WriteLine("[Blazor GlobalStat] START Translations");
            // 12 Sprachen vorinitialisieren
            _languageMaps = new Dictionary<string, string>[12];
            for (int i = 0; i < 12; i++)
            {
                _languageMaps[i] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            //Console.WriteLine($"[Blazor GlobalStat] _languageMaps: {_languageMaps.ToString()}");

            // Index-Mapping für schnellen Zugriff
            string[] codes = { "EN", "DE", "SRL", "SRC", "FR", "IT", "AR", "ZH", "HI", "ES", "ID", "PT" };

            //Console.WriteLine($"[Blazor GlobalStat] codes: {codes.ToString()}");
            for (int i = 0; i < codes.Length; i++)
            {
                _languageIndex[codes[i]] = i;
            }
            //Console.WriteLine($"[Blazor GlobalStat] _languageIndex: {_languageIndex.ToString()}");
        }

        /// <summary>
        /// Gibt das Dictionary für eine Sprache als REFERENZ zurück (keine Kopie!).
        /// </summary>
        public Dictionary<string, string> GetLanguageMap(string languageCode)
        {
            //Console.WriteLine($"[Blazor GlobalStat] START GetLanguageMap , languageCode: {languageCode} , _languageIndex: {_languageIndex.ToString()}");
            if (_languageIndex.TryGetValue(languageCode, out int index))
            {
                //Console.WriteLine($"[Blazor GlobalStat] END GetLanguageMap: {_languageMaps[index].ToString()}");
                return _languageMaps[index];  // REFERENZ!
            }
            //Console.WriteLine($"[Blazor GlobalStat] END GetLanguageMap (Fallback): {_languageMaps[0].ToString()}");
            return _languageMaps[0];  // Fallback EN (Index 0)
        }

        /// <summary>
        /// Interne Methode für das Laden der JSON-Daten.
        /// </summary>
        public Dictionary<string, string> GetLanguageMapByIndex(int index)
        {
            //Console.WriteLine($"[Blazor GlobalStat] START GetLanguageMapByIndex , index: {index} , _languageMaps: {_languageMaps[index].ToString()}");
            return _languageMaps[index];
        }
        
    }

    // Hier die Store-Url's
    //public class DownloadAppModel
    //{
    //    public string? Type { get; set; }
    //    public string? Id { get; set; }
    //    public string? Icon { get; set; }
    //    public string? Title { get; set; }
    //    public string? Url { get; set; }
    //    //public bool IsOpenDropdownDownloadApp = false;

    //    //public string MicrosoftUrl { get; set; } = "https://apps.microsoft.com/search/publisher?name=True%20Perfect%20Code";
    //    //public string GoogleUrl { get; set; } = "https://play.google.com/store/apps/developer?id=True+Perfect+Code";
    //    //public string ApplePhoneUrl { get; set; } = "https://apps.apple.com/us/developer/daniel-simic/id1733470934";
    //    //public string AppleMacUrl { get; set; } = "https://apps.apple.com/us/developer/daniel-simic/id1733470934";
    //    //public string Web { get; set; } = "https://ptodo.true-perfect-code.ch";
    //    //public string MicrosoftUnpackage { get; set; } = "https://apps.microsoft.com/search/publisher?name=True%20Perfect%20Code";
    //    //public string PWA { get; set; } = "https://ptodo.true-perfect-code.ch";

    //}
}

using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using p11.UI;
using p11.UI.Models;
using System.Text.Json.Serialization;

namespace BlazorCore.Services.GlobalState
{
    /// <summary>
    /// Base class for global state models.
    /// Used as a root to prevent trimming of base members.
    /// </summary>
    public class GlobalStateModelBase
    {
    }

    /// <summary>
    /// General application configuration (app name, version, feature flags, etc.).
    /// This configuration is static and identical for all users.
    /// </summary>
    public class ConfigurationGeneral
    {
        // =====================================================================
        // General
        // =====================================================================

        /// <summary>Gets or initializes the application name.</summary>
        public string ApplicationName { get; init; } = string.Empty;

        /// <summary>Gets or initializes the application description.</summary>
        public string ApplicationDescription { get; init; } = string.Empty;

        /// <summary>Gets or initializes the default language code (e.g., "EN").</summary>
        public string DefaultLanguage { get; init; } = string.Empty;

        /// <summary>Gets or initializes comma-separated list of supported language codes.</summary>
        public string AllSupportedLanguageCodes { get; init; } = string.Empty;

        /// <summary>Gets or initializes the application version string.</summary>
        public string AppVersion { get; init; } = string.Empty;

        /// <summary>Gets or initializes the company name.</summary>
        public string Company { get; init; } = string.Empty;

        /// <summary>Gets or initializes the database table schema name.</summary>
        public string TableSchema { get; init; } = string.Empty;

        /// <summary>Gets or initializes the JSON file extension (e.g., ".json").</summary>
        public string FileExtensionJson { get; init; } = string.Empty;

        /// <summary>Gets or initializes the default storage location (Cloud, Local, or Cloud_Local).</summary>
        public STORAGE_LOCATION StorageLocation { get; init; } = STORAGE_LOCATION.CLOUD;

        /// <summary>Gets or sets the local storage type (JSON_HYBRID, MEMORY, SQLITE).</summary>
        public LOCAL_STORAGE_TYPE LocalStorageType { get; set; } = LOCAL_STORAGE_TYPE.JSON_HYBRID;

        /// <summary>Gets or initializes a value indicating whether local storage should be encrypted.</summary>
        public bool LocalStorageEncrypt { get; init; } = false;

        // =====================================================================
        // Hosting
        // =====================================================================

        /// <summary>Gets or initializes the application domain (e.g., "example.com").</summary>
        public string ApplicationDomain { get; init; } = string.Empty;

        /// <summary>Gets or initializes the application URL (client-facing).</summary>
        public string ApplicationUrl { get; init; } = string.Empty;

        /// <summary>Gets or initializes the WebAPI base URL.</summary>
        public string ApplicationApiUrl { get; init; } = string.Empty;

        // =====================================================================
        // Project
        // =====================================================================

        /// <summary>Gets or initializes the AUMID for package identification.</summary>
        public string Aumid { get; init; } = string.Empty;

        /// <summary>Gets or initializes the CLSID for COM identification.</summary>
        public string CLSID { get; init; } = string.Empty;

        /// <summary>Gets or initializes the epoch year for Unix timestamp generation.</summary>
        public int EpochYear { get; init; } = 2026;

        /// <summary>Gets or initializes the Windows border background color (hex).</summary>
        public string WindowsBorderBckGrColor { get; init; } = string.Empty;

        /// <summary>Gets or initializes the Windows border foreground color (hex).</summary> 
        public string WindowsBorderForeColor { get; init; } = string.Empty;

        public bool AllowLocalRegistration { get; init; } = false;

        // =====================================================================
        // Other
        // =====================================================================

        /// <summary>Gets or initializes the task delay for Capacitor bridge operations (ms).</summary>
        public int TaskDelay_Capacitor { get; init; } = 50;

        /// <summary>Gets or initializes a value indicating whether WebAPI communication should be encrypted.</summary>
        public bool EncryptDecryptWebApi { get; init; } = false;

        public bool IsShowingReconnectModal { get; set; } = false;
        public string DefaultFontFamily { get; init; } = string.Empty;

        // =====================================================================
        // Connection
        // =====================================================================

        /// <summary>Gets or initializes the folder name for connection configuration files.</summary>
        public string ConnectionsServerFolder { get; init; } = string.Empty;

        /// <summary>Gets or initializes the connection string.</summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>Gets or initializes the security configuration JSON filename.</summary>
        public string SecurityConfigJsonFilename { get; init; } = string.Empty;

        /// <summary>Gets or initializes the Pepper for app-level security (server).</summary>
        public string PepperApp { get; init; } = string.Empty;

        /// <summary>Gets or initializes the Pepper for app-level security (WASM).</summary>
        public string PepperAppWasm { get; init; } = string.Empty;

        // =====================================================================
        // Debug / Error handling
        // =====================================================================

        /// <summary>Gets or initializes a value indicating whether inspection mode (F12) is enabled in webview.</summary>
        public bool IsInspectionEnabled { get; init; } = false;

        /// <summary>Gets or initializes a value indicating whether debug mode is enabled.</summary>
        public bool IsDebugEnabled { get; init; } = false;

        /// <summary>Gets or initializes the default error message text.</summary>
        public string ErrorText { get; init; } = string.Empty;

        /// <summary>Gets or initializes the WebAPI exception error marker.</summary>
        public string WebapiExceptionError { get; init; } = string.Empty;

        /// <summary>Gets or initializes a value indicating whether connection string should be shown in errors.</summary>
        public bool ShowConnectionStringByError { get; init; } = false;

        /// <summary>Set default device (iphone, android, windows...) to test UI.</summary>
        public NativeDevice? SetDefaultDevice { get; init; } = null;
    }

    /// <summary>
    /// WebAPI endpoint configuration.
    /// Contains URLs and endpoint paths for all API calls.
    /// </summary>
    public class ConfigurationWebapi
    {
        // =====================================================================
        // URLs (full)
        // =====================================================================

        /// <summary>Gets or initializes the URL for TPC user token retrieval.</summary>
        public string url_GetTokenDataTPCuser { get; init; } = string.Empty;

        /// <summary>Gets or initializes the URL for IDP user token retrieval.</summary>
        public string url_GetTokenDataIDPuser { get; init; } = string.Empty;

        /// <summary>Gets or initializes the URL for password change.</summary>
        public string url_ChangePassword { get; init; } = string.Empty;

        /// <summary>Gets or initializes the URL for scalar data queries.</summary>
        public string url_GetScalar { get; init; } = string.Empty;

        /// <summary>Gets or initializes the URL for data modification (non-query).</summary>
        public string url_SetData { get; init; } = string.Empty;

        /// <summary>Gets or initializes the URL for row data queries.</summary>
        public string url_GetRows { get; init; } = string.Empty;

        /// <summary>Gets or initializes the URL for Excel language data.</summary>
        public string url_ExcelLang { get; init; } = string.Empty;

        /// <summary>Gets or initializes the URL for feedback submission.</summary>
        public string url_Feedback { get; init; } = string.Empty;

        /// <summary>Gets or initializes the URL for anonymous API calls.</summary>
        public string url_Anonymous { get; init; } = string.Empty;

        /// <summary>Gets or initializes the URL for Ai calls.</summary>
        public string url_Ai { get; init; } = string.Empty;

        /// <summary>Gets or initializes the URL for authorized connection validation.</summary>
        public string url_Authorizedconnection { get; init; } = string.Empty;

        /// <summary>Gets or initializes the URL for token validation.</summary>
        public string url_CheckToken { get; init; } = string.Empty;

        /// <summary>Gets or initializes the URL for SignalR authentication hub.</summary>
        public string url_AuthHub { get; init; } = string.Empty;

        // =====================================================================
        // Endpoints (paths only)
        // =====================================================================

        /// <summary>Gets or initializes the endpoint path for TPC user token retrieval.</summary>
        public string endpoint_GetTokenDataTPCuser { get; init; } = string.Empty;

        /// <summary>Gets or initializes the endpoint path for IDP user token retrieval.</summary>
        public string endpoint_GetTokenDataIDPuser { get; init; } = string.Empty;

        /// <summary>Gets or initializes the endpoint path for password change.</summary>
        public string endpoint_ChangePassword { get; init; } = string.Empty;

        /// <summary>Gets or initializes the endpoint path for scalar data queries.</summary>
        public string endpoint_GetScalar { get; init; } = string.Empty;

        /// <summary>Gets or initializes the endpoint path for data modification.</summary>
        public string endpoint_SetData { get; init; } = string.Empty;

        /// <summary>Gets or initializes the endpoint path for row data queries.</summary>
        public string endpoint_GetRows { get; init; } = string.Empty;

        /// <summary>Gets or initializes the endpoint path for Excel language data.</summary>
        public string endpoint_ExcelLang { get; init; } = string.Empty;

        /// <summary>Gets or initializes the endpoint path for feedback submission.</summary>
        public string endpoint_Feedback { get; init; } = string.Empty;

        /// <summary>Gets or initializes the endpoint path for anonymous API calls.</summary>
        public string endpoint_Anonymous { get; init; } = string.Empty;

        /// <summary>Gets or initializes the endpoint path for Ai calls.</summary>
        public string endpoint_Ai { get; init; } = string.Empty;

        /// <summary>Gets or initializes the endpoint path for authorized connection validation.</summary>
        public string endpoint_Authorizedconnection { get; init; } = string.Empty;

        /// <summary>Gets or initializes the endpoint path for token validation.</summary>
        public string endpoint_CheckToken { get; init; } = string.Empty;

        /// <summary>Gets or initializes the endpoint path for SignalR authentication hub.</summary>
        public string endpoint_AuthHub { get; init; } = string.Empty;
    }

    /// <summary>
    /// Model for localStorage/secure storage keys.
    /// Each property represents a storage key for a specific setting.
    /// </summary>
    public class LocalStorageModel
    {
        /// <summary>Storage key for storage location preference.</summary>
        public string storagelocation { get; init; } = string.Empty;

        /// <summary>Storage key for OAuth token.</summary>
        public string oauth_token { get; init; } = string.Empty;

        /// <summary>Storage key for language preference.</summary>
        public string language { get; init; } = string.Empty;

        /// <summary>Storage key for LTR/RTL preference.</summary>
        public string ltrrtl { get; init; } = string.Empty;

        /// <summary>Storage key for font family preference.</summary>
        public string fontfamily { get; init; } = string.Empty;

        /// <summary>Storage key for font size preference.</summary>
        public string fontsize { get; init; } = string.Empty;

        /// <summary>Storage key for font weight preference.</summary>
        public string fontweight { get; init; } = string.Empty;

        /// <summary>Storage key for font spacing preference.</summary>
        public string fontspacing { get; init; } = string.Empty;

        /// <summary>Storage key for font line height preference.</summary>
        public string fontlineheight { get; init; } = string.Empty;

        /// <summary>Storage key for theme mode preference.</summary>
        public string thememode { get; init; } = string.Empty;

        /// <summary>Storage key for SQLite encryption key.</summary>
        public string sqlitekey { get; init; } = string.Empty;

        /// <summary>Storage key for last failed 2FA reset timestamp.</summary>
        public string last_failed_reset2fa { get; init; } = string.Empty;

        /// <summary>Storage key for last failed login timestamp.</summary>
        public string last_failed_login { get; init; } = string.Empty;

        /// <summary>Storage key for accessibility settings.</summary>
        public string accessibility { get; init; } = string.Empty;

        /// <summary>Storage key for landing page accessibility settings.</summary>
        public string accessibilitylandingpage { get; init; } = string.Empty;

        /// <summary>Storage key for smart view accessibility settings.</summary>
        public string accessibilitysmartview { get; init; } = string.Empty;

        /// <summary>Storage key for identity provider preference.</summary>
        public string idp { get; init; } = string.Empty;

        /// <summary>Storage key for design settings.</summary>
        public string design { get; init; } = string.Empty;

        /// <summary>Storage key for unknown/unmapped values.</summary>
        public string Unknown { get; init; } = string.Empty;

        /// <summary>Storage key for PIN code.</summary>
        public string pin { get; init; } = string.Empty;

        /// <summary>Storage key for store URLs.</summary>
        public string storeurls { get; init; } = string.Empty;
    }

    /// <summary>
    /// Represents an app download option for different platforms.
    /// </summary>
    public class DownloadAppModel
    {
        /// <summary>Gets or sets the download type (e.g., "store", "web", "desktop").</summary>
        public string? Type { get; set; }

        /// <summary>Gets or sets the unique identifier for the download source.</summary>
        public string? Id { get; set; }

        /// <summary>Gets or sets the icon CSS class (Bootstrap Icons).</summary>
        public string? Icon { get; set; }

        /// <summary>Gets or sets the display title.</summary>
        public string? Title { get; set; }

        /// <summary>Gets or sets the download URL.</summary>
        public string? Url { get; set; }
    }

    /// <summary>
    /// Static application catalog.
    /// Contains configuration that is identical for all users (tables, languages, fonts, etc.).
    /// </summary>
    public class Sections
    {
        /// <summary>Gets or sets the list of MSSQL table names.</summary>
        public List<string>? TablesMSSQL { get; set; }

        /// <summary>Gets or sets the list of table parameter definitions for T-SQL.</summary>
        public List<SqlClient.TableDefinition>? ParaMSSQL { get; set; }

        /// <summary>Gets or initializes the dictionary of available languages (code -> display name).</summary>
        public Dictionary<string, string>? Languages { get; init; }

        /// <summary>Gets or initializes the dictionary of week days.</summary>
        public Dictionary<int, string>? WeekShortest { get; init; }

        /// <summary>Gets or initializes the dictionary of months (jan-dec).</summary>
        public Dictionary<int, string>? Months { get; init; }

        /// <summary>Gets or initializes the list of available font families.</summary>
        public List<FontFamilyModel>? Fonts { get; init; }

        /// <summary>Gets or sets the list of download options for the app.</summary>
        public List<DownloadAppModel>? DownloadApp { get; set; }

        /// <summary>Gets or initializes the localStorage key configuration.</summary>
        public LocalStorageModel? LocalStorage { get; init; }
    }

    /// <summary>
    /// User parameter names for settings and preferences.
    /// </summary>
    public class UserParameterModel
    {
        /// <summary>Gets or initializes the parameter name for debug ID.</summary>
        public string DebugId { get; init; } = string.Empty;

        /// <summary>Gets or initializes the parameter name for storage mode.</summary>
        public string StorageMode { get; init; } = string.Empty;

        /// <summary>Gets or initializes the parameter name for TOP (record count).</summary>
        public string Top { get; init; } = string.Empty;
    }

    /// <summary>
    /// User parameter names that should be hidden from UI.
    /// </summary>
    public class UserParameterHideModel
    {
        /// <summary>Gets or initializes the parameter name for OTP backup code.</summary>
        public string OtpBackupCode { get; init; } = string.Empty;
    }

    /// <summary>
    /// Application dataset containing settings metadata, changelog, and user parameters.
    /// </summary>
    public class Dataset
    {
        /// <summary>Gets or initializes the list of settings metadata definitions.</summary>
        public List<SettingMetadataModel>? Settings { get; init; }

        /// <summary>Gets or initializes the version changelog.</summary>
        public List<VersionChangeLog>? VersionChangeLog { get; init; }

        /// <summary>Gets or initializes the user parameter name mappings.</summary>
        public UserParameterModel? UserParameter { get; init; }

        /// <summary>Gets or initializes the hidden user parameter names.</summary>
        public UserParameterHideModel? UserParameterHide { get; init; }
    }

    /// <summary>
    /// Connection string parameter definitions.
    /// Used for JSON deserialization of connection configuration files.
    /// </summary>
    public class ConnectionStringParametersModel
    {
        /// <summary>Gets or sets the database server hostname.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Server { get; set; }

        /// <summary>Gets or sets the database name.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Database { get; set; }

        /// <summary>Gets or sets the user ID for authentication (when not using integrated security).</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? User_ID { get; set; }

        /// <summary>Gets or sets the password for authentication (encrypted in storage).</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Password { get; set; }

        /// <summary>Gets or sets a value indicating whether connection encryption is required.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Encrypt { get; set; } = false;

        /// <summary>Gets or sets a value indicating whether to use integrated Windows authentication.</summary>
        public bool Integrated_Security { get; set; } = false;

        /// <summary>Gets or sets the connection timeout in seconds.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Connection_Timeout { get; set; } = 0;

        /// <summary>Gets or sets a value indicating whether connection pooling is enabled.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Pooling { get; set; } = true;

        /// <summary>Gets or sets the minimum pool size.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Min_Pool_Size { get; set; } = 0;

        /// <summary>Gets or sets the maximum pool size.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Max_Pool_Size { get; set; } = 0;

        /// <summary>Gets or sets a value indicating whether multiple active result sets are enabled.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool MultipleActiveResultSets { get; set; } = false;

        /// <summary>Gets or sets the application name for the connection.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Application_Name { get; set; }

        /// <summary>Gets or sets a value indicating whether to trust the server certificate.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool TrustServerCertificate { get; set; } = false;

        /// <summary>Gets or sets the current language setting for the connection.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Current_Language { get; set; }

        /// <summary>Gets or sets the packet size in bytes.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Packet_Size { get; set; } = 0;

        /// <summary>Gets or sets the workstation ID.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Workstation_ID { get; set; }

        // =====================================================================
        // Internal fields (not serialized)
        // =====================================================================

        /// <summary>Internal: Show form flag.</summary>
        [JsonIgnore]
        public bool Int__ShowForm { get; set; } = false;

        /// <summary>Internal: Connection file name.</summary>
        [JsonIgnore]
        public string Int__ConnectionFileName { get; set; } = string.Empty;

        /// <summary>Internal: Path type.</summary>
        [JsonIgnore]
        public string Int__Pathtype { get; set; } = string.Empty;

        /// <summary>Internal: Connection string name.</summary>
        [JsonIgnore]
        public string Int__ConnectionStringName { get; set; } = string.Empty;

        /// <summary>Internal: Decrypted password (runtime only).</summary>
        [JsonIgnore]
        public string Int__PasswordDecrypted { get; set; } = string.Empty;

        /// <summary>Clears all connection string properties.</summary>
        public void Clear()
        {
            Server = string.Empty;
            Database = string.Empty;
            User_ID = string.Empty;
            Password = string.Empty;
            Encrypt = false;
            Integrated_Security = false;
            Connection_Timeout = 0;
            Pooling = true;
            Min_Pool_Size = 0;
            Max_Pool_Size = 0;
            MultipleActiveResultSets = false;
            Application_Name = string.Empty;
            TrustServerCertificate = false;
            Current_Language = string.Empty;
            Packet_Size = 0;
            Workstation_ID = string.Empty;

            Int__ConnectionFileName = string.Empty;
            Int__ConnectionStringName = string.Empty;
            Int__PasswordDecrypted = string.Empty;
        }

        /// <summary>Creates a shallow copy of the model.</summary>
        public object Clone() => MemberwiseClone();
    }

    /// <summary>
    /// Represents a single translation entry for all supported languages.
    /// Property names must exactly match the JSON file keys for AOT compatibility.
    /// </summary>
    public class TranslationEntryModel
    {
        /// <summary>English translation (key and fallback).</summary>
        public string EN { get; set; } = string.Empty;

        /// <summary>German translation.</summary>
        public string DE { get; set; } = string.Empty;

        /// <summary>Serbian (Latin) translation.</summary>
        public string SRL { get; set; } = string.Empty;

        /// <summary>Serbian (Cyrillic) translation.</summary>
        public string SRC { get; set; } = string.Empty;

        /// <summary>French translation.</summary>
        public string FR { get; set; } = string.Empty;

        /// <summary>Italian translation.</summary>
        public string IT { get; set; } = string.Empty;

        /// <summary>Arabic translation.</summary>
        public string AR { get; set; } = string.Empty;

        /// <summary>Chinese translation.</summary>
        public string ZH { get; set; } = string.Empty;

        /// <summary>Hindi translation.</summary>
        public string HI { get; set; } = string.Empty;

        /// <summary>Spanish translation.</summary>
        public string ES { get; set; } = string.Empty;

        /// <summary>Indonesian translation.</summary>
        public string ID { get; set; } = string.Empty;

        /// <summary>Portuguese (Brazilian) translation.</summary>
        public string PT { get; set; } = string.Empty;
    }

    /// <summary>
    /// Central, high-performance data structure for all loaded translations.
    /// Uses array indexing instead of dictionary lookups for maximum performance.
    /// </summary>
    public class Translations
    {
        // Internal data structures for maximum performance
        private readonly Dictionary<string, string>[] _languageMaps;
        private readonly Dictionary<string, int> _languageIndex = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the language maps as a dictionary (for compatibility with legacy code).
        /// Returns a copy of the data, not a reference.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="Translations"/> class.
        /// Pre-initializes arrays for all 12 supported languages.
        /// </summary>
        public Translations()
        {
            // Initialize arrays for 12 languages
            _languageMaps = new Dictionary<string, string>[12];
            for (int i = 0; i < 12; i++)
            {
                _languageMaps[i] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            // Map language codes to array indices for O(1) access
            string[] codes = { "EN", "DE", "SRL", "SRC", "FR", "IT", "AR", "ZH", "HI", "ES", "ID", "PT" };

            for (int i = 0; i < codes.Length; i++)
            {
                _languageIndex[codes[i]] = i;
            }
        }

        /// <summary>
        /// Gets the language map for the specified language code as a REFERENCE (no copy).
        /// </summary>
        /// <param name="languageCode">The language code (e.g., "EN", "DE").</param>
        /// <returns>Dictionary reference for the requested language, or English as fallback.</returns>
        public Dictionary<string, string> GetLanguageMap(string languageCode)
        {
            if (_languageIndex.TryGetValue(languageCode, out int index))
            {
                return _languageMaps[index];
            }

            return _languageMaps[0]; // Fallback to English (index 0)
        }

        /// <summary>
        /// Gets the language map by array index (internal use only).
        /// </summary>
        /// <param name="index">The array index (0-11).</param>
        /// <returns>Dictionary reference for the requested language.</returns>
        public Dictionary<string, string> GetLanguageMapByIndex(int index)
        {
            return _languageMaps[index];
        }
    }
}
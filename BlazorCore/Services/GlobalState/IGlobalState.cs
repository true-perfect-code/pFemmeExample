using BlazorCore.Services.AppState;
using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.GlobalState
{
    /// <summary>
    /// Provides globally shared, read-only state across the entire application.
    /// Data in this service is identical for all users (e.g., configuration, translations).
    /// For user-specific state, use <see cref="IAppStateBase"/> instead.
    /// </summary>
    public interface IGlobalStateBase
    {
        // =====================================================================
        // STATIC CONFIGURATION
        // =====================================================================

        /// <summary>
        /// Static application configuration (app name, version, feature flags, etc.).
        /// </summary>
        ConfigurationGeneral ConfigGeneral { get; }

        /// <summary>
        /// Static WebAPI endpoint configuration.
        /// </summary>
        ConfigurationWebapi ConfigWebapi { get; }

        /// <summary>
        /// Static app catalog (tables, languages, fonts, localStorage keys).
        /// </summary>
        Sections Catalog { get; }

        /// <summary>
        /// Initializes the global state with static configuration.
        /// Called once during app startup.
        /// </summary>
        void GlobalInit(ConfigurationGeneral configGeneral, ConfigurationWebapi configWebapi, Sections catalog);

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        /// <summary>
        /// Ensures that the global state is fully initialized.
        /// Loads and caches translations if not already loaded.
        /// </summary>
        Task EnsureInitializedAsync();

        // =====================================================================
        // TRANSLATIONS
        // =====================================================================

        /// <summary>
        /// Gets the centralized, thread-safe collection of all application translations.
        /// </summary>
        Translations Translations { get; }
        
        /// <summary>
        /// Sets additional translation sources.
        /// </summary>
        Task SetTranslations(List<(System.Reflection.Assembly Assembly, string ResourceName)> additionalSources);

        // =====================================================================
        // DATABASE CONNECTION (server-side only)
        // =====================================================================

        /// <summary>
        /// Gets the MSSQL connection string (server-side only).
        /// On client platforms, this returns an empty string.
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Generate connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        Scalar GenerateConnectionString(ConnectionStringModel connectionString);

        // =====================================================================
        // UTILITY METHODS (AOT-safe helpers)
        // =====================================================================

        ///// <summary>
        ///// Converts a string to the specified type in an AOT-safe manner.
        ///// Supports string, bool, int, long, double, decimal, DateTime, DateTimeOffset, Guid, Enum, byte[].
        ///// </summary>
        //T ConvertStrPara<T>(string input, bool forRealm = false);

        /// <summary>
        /// Serializes a dictionary to a JSON string (AOT-safe).
        /// </summary>
        string SerializeDictionaryTpc(Dictionary<string, string> dictionary);

        /// <summary>
        /// Deserializes a JSON string to a dictionary (AOT-safe).
        /// </summary>
        Dictionary<string, string> DeserializeDictionaryTpc(string jsonString);

        /// <summary>
        /// Converts a hexadecimal string to a byte array.
        /// </summary>
        byte[] StringToByteArray(string hex);

        /// <summary>
        /// Parses a Base64 string with automatic padding correction.
        /// </summary>
        byte[] ParseBase64WithoutPadding(string base64);
    }
}

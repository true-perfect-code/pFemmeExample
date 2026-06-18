using System.Text.Json.Serialization;

namespace BlazorCore.Services.Dam
{
    public class DamModelBase
    {
    }

    // Webapi-signalIR Pooling (Userauthentifikation über ext. provider)
    public class AuthTicketDto
    {
        public string WebApiToken { get; set; } = default!;
        public string UnixTS { get; set; } = default!;
    }

    /// <summary>
    /// Generisches Model zum Lesen von Skalarwerten
    /// </summary>
    public class ClientStorageModel
    {
        [JsonPropertyName("WebApiToken")]
        public string WebApiToken { get; set; } = "";

        [JsonPropertyName("UnixTS")]
        public string UnixTS { get; set; } = "";

        [JsonPropertyName("Email")]
        public string Email { get; set; } = "";

        public string Sprache { get; set; } = "EN";

        public string out_err { get; set; } = "";
    }

    /// <summary>
    /// Container für die Otp Verwaltung
    /// </summary>
    public enum MANAGE_OTP : int
    {
        GENERATE,
        DELETE,
        VALIDATE,
        Unknown
    }

    /// <summary>
    /// Defines the available local storage strategies within the p11 framework.
    /// </summary>
    public enum LOCAL_STORAGE_TYPE
    {
        /// <summary>
        /// Automatic selection of the best available local database. 
        /// Native platforms: Uses relational SQLite. 
        /// WASM/PWA: Falls back to JSON_HYBRID (IndexedDB) as SQLite is not available.
        /// </summary>
        LOCAL_DB_AUTO = 0,

        /// <summary>
        /// Represents an encrypted file-per-record JSON storage with RAM caching.
        /// Optimized for performance and flat data structures.
        /// </summary>
        JSON_HYBRID = 1,

        /// <summary>
        /// Data is stored exclusively in volatile RAM. No persistence across application restarts.
        /// </summary>
        MEMORY = 2
    }

    /// <summary>
    /// Centralized database command constants for pEngine.
    /// These constants provide type safety and documentation for database transaction overrides.
    /// </summary>
    public static class DB_CMD
    {
        // --- Global Cloud / Local Switches ---

        /// <summary>
        /// Explicitly forces data to be sent to the cloud storage.
        /// </summary>
        public const string FORCE_CLOUD = "cmd_force_cloud";

        /// <summary>
        /// Explicitly forces data to be saved to the local storage.
        /// </summary>
        public const string FORCE_LOCAL = "cmd_force_local";

        /// <summary>
        /// Prevents data from being sent to the cloud storage.
        /// </summary>
        public const string NO_CLOUD = "cmd_no_cloud";

        /// <summary>
        /// Prevents data from being saved to the local storage.
        /// </summary>
        public const string NO_LOCAL = "cmd_no_local";

        // --- Specific Local Type Overrides (One-way only: SQLite to JSON/Memory) ---

        /// <summary>
        /// Forces the local transaction to use the JSON Hybrid engine. 
        /// Note: This is only available if the JSON cache has been initialized at startup.
        /// </summary>
        public const string FORCE_LOCAL_JSON = "cmd_force_local_json";

        /// <summary>
        /// Forces the local transaction to stay in Memory only.
        /// </summary>
        public const string FORCE_LOCAL_MEMORY = "cmd_force_local_memory";

        /// <summary>
        /// General override key for the local storage type.
        /// </summary>
        public const string LOCAL_TYPE_OVERRIDE = "cmd_local_type";
    }

}

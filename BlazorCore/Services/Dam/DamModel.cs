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
        MEMORY = 2,
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

        /// <summary>
        /// Prevents to create user.
        /// </summary>
        public const string NO_CREATE_USER = "cmd_nocreateuser";

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
        /// Forces the local storage to synchronize and update credentials (e.g., password hash)
        /// even if local validation would otherwise fail.
        /// </summary>
        public const string FORCE_UPDATE_LOCAL_CREDENTIALS = "cmd_force_update_local_credentials";

        /// <summary>
        /// General override key for the local storage type.
        /// </summary>
        public const string LOCAL_TYPE_OVERRIDE = "cmd_local_type";

        /// <summary>
        /// General override key for the local storage type.
        /// </summary>
        public const string ALLOW_LOCAL_REGISTRATION = "cmd_allow_local:registration";

        // --- Error Message Codes ---

        /// <summary>
        /// Error code indicating no internet connection is available.
        /// </summary>
        public const string ERR_NO_INTERNET_CONNECTION = "no_internet_connection";

        /// <summary>
        /// Error code indicating that a local account could not be created.
        /// </summary>
        public const string ERR_NO_LOCAL_ACCOUNT_CREATED = "no_local_account_created";

        // --- AI (Artificial Intelligence) Cases & Parameters ---

        /// <summary>
        /// AI Case: Performs a chat completion using Azure OpenAI.
        /// Expects AI_SYSTEM_PROMPT and AI_USER_PROMPT parameters.
        /// Optional: AI_TEMPERATURE, AI_MAX_TOKENS, AI_MODEL.
        /// </summary>
        public const string AI_COMPLETE_CHAT = "AiCompleteChat";

        /// <summary>
        /// Alternative AI Case name for backward compatibility with existing code.
        /// Performs a chat completion using Azure OpenAI.
        /// </summary>
        public const string AI_COMPLETE_CHAT_ALT = "CompleteChatAsync";

        /// <summary>
        /// AI Parameter: Defines the system prompt that controls AI behavior, role, and constraints.
        /// Example: "You are a helpful medical assistant."
        /// </summary>
        public const string AI_SYSTEM_PROMPT = "ai_system_prompt";

        /// <summary>
        /// AI Parameter: The actual user input or instruction for the AI.
        /// Example: "Analyze my menstrual cycle data and provide insights."
        /// </summary>
        public const string AI_USER_PROMPT = "ai_user_prompt";

        /// <summary>
        /// AI Parameter: Controls randomness of AI responses.
        /// Range: 0.0 (deterministic) to 1.0 (creative/random).
        /// Default: 0.7
        /// </summary>
        public const string AI_TEMPERATURE = "ai_temperature";

        /// <summary>
        /// AI Parameter: Maximum number of tokens in the AI response.
        /// Default: 500
        /// </summary>
        public const string AI_MAX_TOKENS = "ai_max_tokens";

        /// <summary>
        /// AI Parameter: Allows overriding the default AI model.
        /// Example: "gpt-4", "gpt-35-turbo", "gpt-4-turbo"
        /// </summary>
        public const string AI_MODEL = "ai_model";

        // --- Future AI Extensions (reserved for later implementation) ---

        /// <summary>
        /// AI Case: Reserved for future embedding generation.
        /// </summary>
        public const string AI_EMBEDDING = "AiEmbedding";

        /// <summary>
        /// AI Case: Reserved for future text summarization.
        /// </summary>
        public const string AI_SUMMARIZE = "AiSummarize";

        /// <summary>
        /// AI Case: Reserved for future RAG (Retrieval-Augmented Generation) pipeline.
        /// </summary>
        public const string AI_RAG = "AiRag";

        /// <summary>
        /// AI Case: Reserved for future speech-to-text or text-to-speech.
        /// </summary>
        public const string AI_SPEECH = "AiSpeech";

        /// <summary>
        /// AI Case: Reserved for future translation services.
        /// </summary>
        public const string AI_TRANSLATION = "AiTranslation";
    }

    public static class DB_RES
    {
        public const string NO_USER = "no_user";
    }

}

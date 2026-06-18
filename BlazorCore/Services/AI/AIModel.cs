using System.Collections.Generic;

namespace BlazorCore.Services.AI
{
    /// <summary>
    /// Internal placeholder for future AI domain extensions.
    /// Reserved for embeddings, tool calling, function definitions, etc.
    /// </summary>
    internal class AIModel
    {
    }

    /// <summary>
    /// Represents a generic AI request used by the BlazorCore AI engine.
    /// Fully provider-agnostic and reusable across all platforms.
    /// </summary>
    public class AIRequest
    {
        /// <summary>
        /// Defines system behavior, role, and constraints for the AI.
        /// </summary>
        public string SystemPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Actual user input / instruction.
        /// </summary>
        public string UserPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Optional metadata for extensibility (tenant, model override, tracing, etc.).
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Provider-agnostic configuration for AI services.
    /// Used to initialize AI engine without dependency on appsettings.json.
    /// </summary>
    public class AIConfiguration
    {
        /// <summary>AI endpoint (Azure OpenAI, OpenAI, etc.).</summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>API key for authentication.</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Model deployment name.</summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>Optional tenant identifier.</summary>
        public string? TenantId { get; set; }

        /// <summary>Optional temperature setting (0.0 - 1.0).</summary>
        public double? Temperature { get; set; }
    }

    /// <summary>
    /// AI-specific DB_CMD constants for DAM integration.
    /// These constants are used in the db_para dictionary when calling _dam.Ai().
    /// </summary>
    public static class AI_CMD
    {
        // --- AI Cases (for @Case_ parameter) ---
        /// <summary>
        /// Chat completion with Azure OpenAI
        /// </summary>
        public const string COMPLETE_CHAT = "AiCompleteChat";

        /// <summary>
        /// Alternative case name for backward compatibility
        /// </summary>
        public const string COMPLETE_CHAT_ALT = "CompleteChatAsync";

        // --- AI Parameter Keys ---
        /// <summary>
        /// System prompt that defines AI behavior
        /// </summary>
        public const string SYSTEM_PROMPT = "ai_system_prompt";

        /// <summary>
        /// User prompt with the actual question/instruction
        /// </summary>
        public const string USER_PROMPT = "ai_user_prompt";

        /// <summary>
        /// Temperature (0.0 - 1.0) controlling randomness
        /// </summary>
        public const string TEMPERATURE = "ai_temperature";

        /// <summary>
        /// Maximum tokens in the response
        /// </summary>
        public const string MAX_TOKENS = "ai_max_tokens";

        /// <summary>
        /// Model override (e.g., "gpt-4", "gpt-35-turbo")
        /// </summary>
        public const string MODEL = "ai_model";
    }
}
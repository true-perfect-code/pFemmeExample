using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.AI
{
    /// <summary>
    /// Defines a generic AI service contract for executing AI requests
    /// independent of any provider (Azure OpenAI, OpenAI, local LLM, etc.).
    /// </summary>
    public interface IAI
    {
        /// <summary>
        /// Initializes the AI service with external configuration (endpoint, keys, model settings, etc.).
        /// This allows the service to remain independent from appsettings.json and hosting environment.
        /// </summary>
        /// <param name="config">AI configuration abstraction.</param>
        Task ConfigInitializeAsync(AIConfiguration config);

        /// <summary>
        /// Executes an AI request using AIRequest object (direct call, not via DAM).
        /// </summary>
        Task<ScalarModel> ExecuteAsync(AIRequest request);

        /// <summary>
        /// Executes an AI request using DAM-style Dictionary (called by _dam.Ai()).
        /// This is the primary entry point for DAM-integrated AI calls.
        /// </summary>
        /// <param name="db_para">Dictionary with @Case_ and AI parameters</param>
        Task<ScalarModel> ExecuteAsync(Dictionary<string, string> db_para);
    }
}
using Azure.AI.OpenAI;
using BlazorCore.Services.Dam;
using BlazorCore.Services.SqlClient;
using OpenAI.Chat;
using System.Text.Json;

namespace BlazorCore.Services.AI
{
    /// <summary>
    /// Core AI engine implementation for Azure OpenAI.
    /// Can be called either directly (via AIRequest) or through DAM (via Dictionary).
    /// </summary>
    public class AI : IAI
    {
        private AIConfiguration? _config;
        private AzureOpenAIClient? _azureClient;
        private ChatClient? _chatClient;

        /// <summary>
        /// Initializes the AI service with external configuration.
        /// </summary>
        public Task ConfigInitializeAsync(AIConfiguration config)
        {
            _config = config;

            if (_config != null && !string.IsNullOrEmpty(_config.Endpoint) && !string.IsNullOrEmpty(_config.ApiKey))
            {
                _azureClient = new AzureOpenAIClient(
                    new Uri(_config.Endpoint),
                    new System.ClientModel.ApiKeyCredential(_config.ApiKey)
                );
                _chatClient = _azureClient.GetChatClient(_config.ModelName);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes an AI request using AIRequest object (direct call).
        /// </summary>
        public async Task<ScalarModel> ExecuteAsync(AIRequest request)
        {
            if (_chatClient == null || _config == null)
            {
                return new ScalarModel
                {
                    out_err = "AI service not initialized (ConfigInitializeAsync missing or invalid)."
                };
            }

            try
            {
                // Extract parameters from request
                double temperature = _config.Temperature ?? 0.7;
                int maxTokens = 500;

                if (request.Metadata != null)
                {
                    if (request.Metadata.TryGetValue("temperature", out var tempObj) && tempObj is double temp)
                        temperature = temp;
                    if (request.Metadata.TryGetValue("max_tokens", out var tokenObj) && tokenObj is int tokens)
                        maxTokens = tokens;
                }

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(request.SystemPrompt),
                    new UserChatMessage(request.UserPrompt)
                };

                var chatOptions = new ChatCompletionOptions
                {
                    Temperature = (float)temperature,
                    MaxOutputTokenCount = maxTokens
                };

                var completion = await _chatClient.CompleteChatAsync(messages, chatOptions);
                var response = completion.Value.Content[0].Text;

                return new ScalarModel
                {
                    out_err = string.Empty,
                    out_value_str = response
                };
            }
            catch (Exception ex)
            {
                return new ScalarModel
                {
                    out_err = $"AI execution failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Executes an AI request using DAM-style Dictionary (called by _dam.Ai()).
        /// Supported @Case_ values:
        /// - "AiCompleteChat" or "CompleteChatAsync" → Chat completion
        /// </summary>
        public async Task<ScalarModel> ExecuteAsync(Dictionary<string, string> db_para)
        {
            if (_chatClient == null || _config == null)
            {
                return new ScalarModel
                {
                    out_err = "AI service not initialized (ConfigInitializeAsync missing or invalid)."
                };
            }

            try
            {
                // Extract @Case_ to determine operation type
                if (!db_para.TryGetValue("@Case_", out string? caseValue) || string.IsNullOrEmpty(caseValue))
                {
                    return new ScalarModel
                    {
                        out_err = "Missing @Case_ parameter in AI request."
                    };
                }

                switch (caseValue)
                {
                    case "AiCompleteChat":
                    case "CompleteChatAsync":
                        return await ExecuteChatCompletionAsync(db_para);

                    default:
                        return new ScalarModel
                        {
                            out_err = $"Unknown AI case: {caseValue}"
                        };
                }
            }
            catch (Exception ex)
            {
                return new ScalarModel
                {
                    out_err = $"AI execution failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Executes a chat completion using parameters from Dictionary.
        /// </summary>
        private async Task<ScalarModel> ExecuteChatCompletionAsync(Dictionary<string, string> db_para)
        {
            // Extract required prompts
            string systemPrompt = db_para.ContainsKey(DB_CMD.AI_SYSTEM_PROMPT)
                ? db_para[DB_CMD.AI_SYSTEM_PROMPT]
                : "You are a helpful assistant.";

            string userPrompt = db_para.ContainsKey(DB_CMD.AI_USER_PROMPT)
                ? db_para[DB_CMD.AI_USER_PROMPT]
                : string.Empty;

            if (string.IsNullOrEmpty(userPrompt))
            {
                return new ScalarModel
                {
                    out_err = "Missing user prompt (AI_USER_PROMPT) in AI request."
                };
            }

            // Extract optional parameters
            double temperature = 0.7;
            if (db_para.TryGetValue(DB_CMD.AI_TEMPERATURE, out string? tempStr) &&
                double.TryParse(tempStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedTemp))
            {
                temperature = Math.Clamp(parsedTemp, 0.0, 1.0);
            }

            int maxTokens = 500;
            if (db_para.TryGetValue(DB_CMD.AI_MAX_TOKENS, out string? tokenStr) &&
                int.TryParse(tokenStr, out int parsedTokens))
            {
                maxTokens = parsedTokens;
            }

            // Optional: model override
            string? modelOverride = db_para.ContainsKey(DB_CMD.AI_MODEL) ? db_para[DB_CMD.AI_MODEL] : null;

            ChatClient? clientToUse = _chatClient;
            if (!string.IsNullOrEmpty(modelOverride) && modelOverride != _config!.ModelName)
            {
                clientToUse = _azureClient?.GetChatClient(modelOverride);
                if (clientToUse == null)
                {
                    return new ScalarModel
                    {
                        out_err = $"Invalid model override: {modelOverride}"
                    };
                }
            }

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = (float)temperature,
                MaxOutputTokenCount = maxTokens
            };

            var completion = await clientToUse.CompleteChatAsync(messages, chatOptions);
            string response = completion.Value.Content[0].Text;

            // Optional: Token usage can be stored in additional ScalarModel fields if needed
            // Example: out_value_int = completion.Value.Usage?.OutputTokenCount ?? 0

            return new ScalarModel
            {
                out_err = string.Empty,
                out_value_str = response

                // Optional: if you want to return token usage:
                // out_value_int = completion.Value.Usage?.OutputTokenCount ?? 0,
                // out_value_long = completion.Value.Usage?.TotalTokenCount ?? 0
            };
        }
    }
}
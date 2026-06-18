using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using BlazorCore.Services.SqlClient;
using pFemmeExample.Shared.Models;

namespace pFemmeExample.Shared.Services.Components
{
    public interface IAIService
    {
        Task<ScalarModel> AskAI(string systemPrompt, string userPrompt);
        Task<ScalarModel> AvailableAi();
    }

    public class AIService : IAIService
    {
        private readonly IAppStateBase _appState;
        private readonly IDamBase _dam;
        private readonly LogicService _logicService;

        // Alle Abhängigkeiten des Originalcodes über DI injizieren
        public AIService(IAppStateBase appState, IDamBase dam, LogicService logicService)
        {
            _appState = appState;
            _dam = dam;
            _logicService = logicService;
        }

        /// <summary>
        /// Lädt einen Datensatz aus der Datenbank.
        /// </summary>
        /// <param name="AuthUsers_UnixTS">UnixTS Kennung des Datensatzes</param>
        /// <param name="RecordDate">Aktuelles Client-Datum</param>
        /// <param name="IsGrouping">Sollen die Daten gruppiert werden</param>
        /// <returns>ReadModel</returns>
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> AskAI(string systemPrompt, string userPrompt)
        {
            BlazorCore.Services.SqlClient.ScalarModel result = new();

            try
            {
                var db_para = new Dictionary<string, string>
                {
                    { "@Case_", DB_CMD.AI_COMPLETE_CHAT },
                    { DB_CMD.AI_SYSTEM_PROMPT, systemPrompt },
                    { DB_CMD.AI_USER_PROMPT, userPrompt },
                    { DB_CMD.AI_TEMPERATURE, "0.7" },
                    { DB_CMD.AI_MAX_TOKENS, "1000" }
                };

                result = await _dam.Ai(db_para);
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
        }

        public async Task<BlazorCore.Services.SqlClient.ScalarModel> AvailableAi()
        {
            BlazorCore.Services.SqlClient.ScalarModel result = new();

            try
            {
                var unixTS = _appState.GenerateUniqueId();

                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "AvailableAi>>AppParameter" },
                    { "@UnixTS", unixTS },
                    { "@AuthUsers_UnixTS", _appState.UnixTS },
                    { DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString() },
                };

                result = await _dam.Scalar(db_para);
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
        }

    }
}




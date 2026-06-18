using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using pFemmeExample.Shared.Models;

namespace pFemmeExample.Shared.Services.Components
{
    public class CyclePhasesService
    {
        private readonly LogicService _logicService;

        // Alle Abhängigkeiten des Originalcodes über DI injizieren
        public CyclePhasesService(LogicService logicService)
        {
            _logicService = logicService;
        }

        /// <summary>
        /// Lädt einen Datensatz aus der Datenbank.
        /// </summary>
        /// <param name="AuthUsers_UnixTS">UnixTS Kennung des Datensatzes</param>
        /// <param name="RecordDate">Aktuelles Client-Datum</param>
        /// <param name="IsGrouping">Sollen die Daten gruppiert werden</param>
        /// <returns>ReadModel</returns>
        public async Task<BlazorCore.Services.SqlClient.ReadModel<CycleSummaryModel?>> LoadCycleHistory()
        {
            BlazorCore.Services.SqlClient.ReadModel<CycleSummaryModel?> result = new();
            try
            {
                result = await _logicService.SelectCycleHistory();
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }
            return result ?? new BlazorCore.Services.SqlClient.ReadModel<CycleSummaryModel?>();
        }

    }
}


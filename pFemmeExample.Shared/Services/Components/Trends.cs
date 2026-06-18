using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using pFemmeExample.Shared.Models;

namespace pFemmeExample.Shared.Services.Components
{
    public class TrendsService
    {
        private readonly IAppStateBase _appState;
        private readonly IDamBase _dam;
        private readonly LogicService _logicService;

        // Alle Abhängigkeiten des Originalcodes über DI injizieren
        public TrendsService(IAppStateBase appState, IDamBase dam, LogicService logicService)
        {
            _appState = appState;
            _dam = dam;
            _logicService = logicService;
        }

        /// <summary>
        /// Lädt einen Datensatz aus der Datenbank.
        /// </summary>
        /// <param name="Todo_UnixTS">UnixTS Kennung des Datensatzes</param>
        /// <returns>ReadModel</returns>
        public async Task<BlazorCore.Services.SqlClient.ReadModel<ChartsModel?>> LoadChartBleeding()
        {
            BlazorCore.Services.SqlClient.ReadModel<ChartsModel?> result = new();

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "SelectTrendsBleeding>>Cycles" },
                    { "@AuthUsers_UnixTS", _appState.UnixTS },
                };

                result = await _dam.ReadData<ChartsModel>(db_para)!;
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result ?? new BlazorCore.Services.SqlClient.ReadModel<ChartsModel?>();
        }

        /// <summary>
        /// Lädt einen Datensatz aus der Datenbank.
        /// </summary>
        /// <param name="Todo_UnixTS">UnixTS Kennung des Datensatzes</param>
        /// <returns>ReadModel</returns>
        public async Task<BlazorCore.Services.SqlClient.ReadModel<ChartsModel?>> LoadChartPain()
        {
            BlazorCore.Services.SqlClient.ReadModel<ChartsModel?> result = new();

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "SelectTrendsPain>>Cycles" },
                    { "@AuthUsers_UnixTS", _appState.UnixTS },
                };

                result = await _dam.ReadData<ChartsModel>(db_para)!;
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result ?? new BlazorCore.Services.SqlClient.ReadModel<ChartsModel?>();
        }

        /// <summary>
        /// Lädt einen Datensatz aus der Datenbank.
        /// </summary>
        /// <param name="Todo_UnixTS">UnixTS Kennung des Datensatzes</param>
        /// <param name="RecordDate">Aktuelles Client-Datum</param>
        /// <returns>ReadModel</returns>
        public async Task<BlazorCore.Services.SqlClient.ReadModel<ChartsModel?>> LoadChartCycle()
        {
            BlazorCore.Services.SqlClient.ReadModel<ChartsModel?> result = new();

            try
            {
                result = await _logicService.SelectTrendsCycle();
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result ?? new BlazorCore.Services.SqlClient.ReadModel<ChartsModel?>();
        }

        /// <summary>
        /// Lädt Median gerechnete Differenz zwischen Blutungsbegin-Tagen (in der Regel 28 Tage).
        /// </summary>
        /// <param name="Todo_UnixTS">UnixTS Kennung des Datensatzes</param>
        /// <param name="RecordDate">Aktuelles Client-Datum</param>
        /// <returns>ReadModel</returns>
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> LoadMedianCycleLength()
        {
            BlazorCore.Services.SqlClient.ScalarModel result = new();

            try
            {
                result = await _logicService.LoadMedianCycleLength();
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
        }
    }
}


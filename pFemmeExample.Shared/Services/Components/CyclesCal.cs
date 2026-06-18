using pFemmeExample.Shared.Models;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;

namespace pFemmeExample.Shared.Services.Components
{
    public class CyclesCalService
    {
        private readonly IAppStateBase _appState;
        private readonly IDamBase _dam;

        // Alle Abhängigkeiten des Originalcodes über DI injizieren
        public CyclesCalService(IAppStateBase appState, IDamBase dam)
        {
            _appState = appState;
            _dam = dam;
        }

        /// <summary>
        /// Lädt alle Daten aus der Datenbank.
        /// </summary>
        /// <returns>ReadModel</returns>
        public async Task<BlazorCore.Services.SqlClient.ReadModel<CyclesModel?>> Load()
        {
            BlazorCore.Services.SqlClient.ReadModel<CyclesModel?> result = new();

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "Select>>Cycles" },
                    { "@AuthUsers_UnixTS", _appState.UnixTS },
                };

                result = await _dam.ReadData<CyclesModel>(db_para)!;
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result ?? new BlazorCore.Services.SqlClient.ReadModel<CyclesModel?>();
        }

        /// <summary>
        /// Lädt einen bestimmten Tag aus der Datenbank.
        /// </summary>
        /// <param name="date">UnixTS Kennung des Datensatzes</param>
        /// <returns>ReadModel</returns>
        public async Task<BlazorCore.Services.SqlClient.ReadModel<CyclesModel?>> LoadDay(DateTime date)
        {
            BlazorCore.Services.SqlClient.ReadModel<CyclesModel?> result = new();

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "SelectDay>>Cycles" },
                    { "@RecordDate", date.ToString("yyyy-MM-dd") },
                    { "@AuthUsers_UnixTS", _appState.UnixTS },
                };

                result = await _dam.ReadData<CyclesModel>(db_para)!;
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result ?? new BlazorCore.Services.SqlClient.ReadModel<CyclesModel?>();
        }

    }
}

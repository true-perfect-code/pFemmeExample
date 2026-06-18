using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using pFemmeExample.Shared.Models;

namespace pFemmeExample.Shared.Services.Components
{
    public class CyclesService
    {
        private readonly IAppStateBase _appState;
        private readonly IDamBase _dam;

        public CyclesService(IAppStateBase appState, IDamBase dam)
        {
            _appState = appState;
            _dam = dam;
        }
     
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> Save(
           CyclesModel item,
           STORAGE_LOCATION storagelocation = STORAGE_LOCATION.CLOUD_LOCAL)
        {
            var result = new BlazorCore.Services.SqlClient.ScalarModel();

            try
            {
                DateTime now = DateTime.Now;

                // Defaulting nur einmal
                item.RecordDate ??= now;
                item.created_at ??= now;
                item.updated_at ??= now;

                item.UnixTS = string.IsNullOrEmpty(item.UnixTS)
                    ? _appState.GenerateUniqueId()
                    : item.UnixTS;

                var db_para = new Dictionary<string, string>
                {
                    { "@Case_", "Save>>Cycles" },
                    { "@UnixTS", item.UnixTS },
                    { "@Details", item.Details },
                    { "@intensity", item.intensity.ToString() },
                    { "@pain", item.pain.ToString() },
                    { "@headache", item.headache.ToString() },
                    { "@fatigue", item.fatigue.ToString() },
                    { "@nausea", item.nausea.ToString() },
                    { "@cramps", item.cramps.ToString() },
                    { "@bleeding", item.bleeding ? "1" : "0" },

                    { "@RecordDate", item.RecordDate.Value.ToString("yyyy-MM-dd") },
                    { "@created_at", item.created_at.Value.ToString("yyyy-MM-dd") },
                    { "@updated_at", item.updated_at.Value.ToString("yyyy-MM-dd") },

                    { "@AuthUsers_UnixTS", _appState.UnixTS },
                };

                switch (storagelocation)
                {
                    case STORAGE_LOCATION.CLOUD:
                        db_para.Add(DB_CMD.NO_LOCAL, DB_CMD.NO_LOCAL.ToString());
                        db_para.Add("@LastUpdateUnixTS", item.LastUpdateUnixTS.ToString());
                        break;
                    case STORAGE_LOCATION.LOCAL:
                        db_para.Add(DB_CMD.NO_CLOUD, DB_CMD.NO_CLOUD.ToString());
                        db_para.Add("@LastUpdateUnixTS", item.LastUpdateUnixTS.ToString());
                        break;
                }

                result = await _dam.Save(db_para);
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Deletes all cycles by account (requires AuthUsers_UnixTS for security).
        /// </summary>
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> Delete(string authUsersUnixTS)
        {
            BlazorCore.Services.SqlClient.ScalarModel result = new();

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "Delete>>Cycles" },
                    { "@AuthUsers_UnixTS", authUsersUnixTS },
                };

                result = await _dam.ExecQuery(db_para);
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
        }

        /// <summary>
        /// Deletes a cycle by its UnixTS (requires AuthUsers_UnixTS for security).
        /// </summary>
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> DeleteUnixTS(string unixTS, string authUsersUnixTS)
        {
            BlazorCore.Services.SqlClient.ScalarModel result = new();

            try
            {
                Dictionary<string, string> db_para = new()
                {
                    { "@Case_", "DeleteUnixTS>>Cycles" },
                    { "@UnixTS", unixTS },
                    { "@AuthUsers_UnixTS", authUsersUnixTS },
                };

                result = await _dam.ExecQuery(db_para);
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
        }

        /// <summary>
        /// Deletes a cycle using the model (uses AppState for AuthUsers_UnixTS).
        /// </summary>
        public async Task<BlazorCore.Services.SqlClient.ScalarModel> Delete(CyclesModel item)
        {
            return await DeleteUnixTS(item.UnixTS!, _appState.UnixTS);
        }

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

        public async Task<BlazorCore.Services.SqlClient.ReadModel<CyclesCompareModel?>> CompareLocalCloud(List<CyclesCompareModel?> items_cloud, List<CyclesCompareModel?> items_local)
        {
            BlazorCore.Services.SqlClient.ReadModel<CyclesCompareModel?> result = new();

            try
            {
                var result_cycle_compare_list = await _dam.CompareCloudLocalData<CyclesCompareModel>(items_cloud, items_local);
                if (result_cycle_compare_list != null)
                {
                    result.out_list = result_cycle_compare_list;
                }
            }
            catch (Exception ex)
            {
                result.out_err = ex.Message;
            }

            return result ?? new BlazorCore.Services.SqlClient.ReadModel<CyclesCompareModel?>();
        }


    }
}

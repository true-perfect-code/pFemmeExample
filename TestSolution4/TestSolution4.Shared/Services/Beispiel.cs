//using Shared.Services.AppState;
//using BlazorCore.DbApp.Models;
//using BlazorCore.Services.AppState;
//using BlazorCore.Services.Dam;

//namespace TestSolution4.Shared.Services
//{
//    public class TasksService
//    {
//        private readonly IAppState _appState;
//        private readonly IDamBase _dam;

//        // Alle Abhängigkeiten des Originalcodes über DI injizieren
//        public TasksService(IAppState appState, IDamBase dam)
//        {
//            _appState = appState;
//            _dam = dam;
//        }

//        // BEISPIEL //////////////////////////////////////////////////////////////////
//        /// <summary>
//        /// Speichert einen Tasks-Eintrag in der Datenbank.
//        /// </summary>
//        /// <param name="item">Das Tasks-Model, das gespeichert werden soll.</param>
//        /// <returns>ScalarModel</returns>
//        public async Task<BlazorCore.Services.SqlClient.ScalarModel> Save(TasksModel item, STORAGE_LOCATION storagelocation = STORAGE_LOCATION.CLOUD_LOCAL)
//        {
//            BlazorCore.Services.SqlClient.ScalarModel result = new();
//            try
//            {
//                // UnixTS ermitteln
//                item.UnixTS = string.IsNullOrEmpty(item.UnixTS) ? _appState.GenerateUniqueId() : item.UnixTS;

//                await Task.Delay(5);
//                string UnixTS2 = _appState.GenerateUniqueId();

//                // Parameter setzen
//                Dictionary<string, string> db_para = new()
//                {
//                    { "@Case_", "Save>>Tasks" },
//                    { "@UnixTS", item.UnixTS },
//                    { "@DisplayName", item.DisplayName },
//                    { "@IsChecked", item.IsChecked ? "1" : "0" },
//                    { "@imgJpeg", item.imgJpeg },
//                    { "@imgJpegThumbnail", item.imgJpegThumbnail },
//                    { "@Todo_UnixTS", item.Todo_UnixTS },
//                    { "@AuthUsers_UnixTS", _appState.UnixTS },
//                };

//                // Wichtig wegen Migration
//                if (item.Int__MigrationToMSSQL || item.Int__MigrationToSqLite)
//                {
//                    //db_para["@LastUpdateUnixTS"] = item.LastUpdateUnixTS.ToString(); // Der migrierte Datensazu muss gleiche LastUpdateUnixTS haben
//                    //if (item.Int__LastUpdateUnixTSmig > 0)
//                    //    db_para["@LastUpdateUnixTS2"] = item.Int__LastUpdateUnixTSmig.ToString();
                    
//                    db_para["@LastUpdateUnixTS"] = item.LastUpdateUnixTS > 0 ? item.LastUpdateUnixTS.ToString() : DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
//                    db_para["@IsMigration"] = "1"; // Damit die 'LastUpdateUnixTS' Aktualisierung bei Todo verhindert wird!
//                    //db_para["@LastUpdateUnixTS2"] = LastUpdateUnixTS.ToString();
//                }
                    

//                _dam.SetMigrationFlags(ref db_para, storagelocation); // Migartionsflags setzen
//                result = await _dam.Save(db_para)!; // Abfrage ausführen
//                if (result != null && !string.IsNullOrEmpty(result.out_err)) // Resultat prüfen
//                    return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
//            }
//            catch (Exception ex)
//            {
//                result.out_err = ex.Message;
//            }

//            return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
//        }

//        // BEISPIEL //////////////////////////////////////////////////////////////////
//        public async Task<BlazorCore.Services.SqlClient.ScalarModel> SaveIsChecked(TasksModel item, bool isChecked, STORAGE_LOCATION storagelocation = STORAGE_LOCATION.CLOUD_LOCAL)
//        {
//            BlazorCore.Services.SqlClient.ScalarModel result = new();
//            try
//            {
//                // UnixTS ermitteln
//                item.UnixTS = string.IsNullOrEmpty(item.UnixTS) ? _appState.GenerateUniqueId() : item.UnixTS;

//                await Task.Delay(5);
//                string UnixTS2 = _appState.GenerateUniqueId();

//                // Parameter setzen
//                Dictionary<string, string> db_para = new()
//                {
//                    { "@Case_", "UpdateIsChecked>>Tasks" },
//                    { "@UnixTS", item.UnixTS },
//                    { "@AuthUsers_UnixTS", _appState.UnixTS },
//                    { "@Todo_UnixTS", item.Todo_UnixTS! },
//                    { "@IsChecked", isChecked ? "1" : "0" },
//                };
//                _dam.SetMigrationFlags(ref db_para, storagelocation); // Migartionsflags setzen
//                result = await _dam.Save(db_para)!; // Abfrage ausführen
//                if (result != null && !string.IsNullOrEmpty(result.out_err)) // Resultat prüfen
//                    return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
//            }
//            catch (Exception ex)
//            {
//                result.out_err = ex.Message;
//            }

//            return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
//        }

//        // BEISPIEL //////////////////////////////////////////////////////////////////
//        public async Task<BlazorCore.Services.SqlClient.ScalarModel> Delete(TasksModel item)
//        {
//            BlazorCore.Services.SqlClient.ScalarModel result = new();
//            try
//            {
//                // Parameter setzen
//                Dictionary<string, string> db_para = new()
//                {
//                    { "@Case_", "Delete>>Tasks" },
//                    { "@UnixTS", item.UnixTS! },
//                    { "@Todo_UnixTS", item.Todo_UnixTS! },
//                };
//                result = await _dam.ExecQuery(db_para)!; // Abfrage ausführen
//                if (result != null && string.IsNullOrEmpty(result.out_err)) // Resultat prüfen
//                    return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
//            }
//            catch (Exception ex)
//            {
//                result.out_err = ex.Message;
//            }
//            return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
//        }

//        // BEISPIEL //////////////////////////////////////////////////////////////////
//        public async Task<BlazorCore.Services.SqlClient.ScalarModel> DeleteByTodo(string Todo_UnixTS)
//        {
//            BlazorCore.Services.SqlClient.ScalarModel result = new();
//            try
//            {
//                // Parameter setzen
//                Dictionary<string, string> db_para = new()
//                {
//                    { "@Case_", "DeleteTodoTasks>>Tasks" },
//                    { "@Todo_UnixTS", Todo_UnixTS },
//                };
//                result = await _dam.ExecQuery(db_para)!; // Abfrage ausführen
//                if (result != null && string.IsNullOrEmpty(result.out_err)) // Resultat prüfen
//                    return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
//            }
//            catch (Exception ex)
//            {
//                result.out_err = ex.Message;
//            }
//            return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
//        }

//        // BEISPIEL //////////////////////////////////////////////////////////////////
//        public async Task<BlazorCore.Services.SqlClient.ScalarModel> LoadImgJpeg(TasksModel item)
//        {
//            BlazorCore.Services.SqlClient.ScalarModel result = new();
//            try
//            {
//                // Parameter setzen
//                Dictionary<string, string> db_para = new()
//                {
//                    { "@Case_", "SelectImgJpeg>>Tasks" },
//                    { "@UnixTS", item.UnixTS },
//                };
//                result = await _dam.Scalar(db_para)!; // Abfrage ausführen
//                if (result != null && string.IsNullOrEmpty(result.out_err)) // Resultat prüfen
//                    return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
//            }
//            catch (Exception ex)
//            {
//                result.out_err = ex.Message;
//            }
//            return result ?? new BlazorCore.Services.SqlClient.ScalarModel();
//        }

//        // BEISPIEL //////////////////////////////////////////////////////////////////
//        public async Task<BlazorCore.Services.SqlClient.ReadModel<TasksModel?>> LoadByTodo_UnixTS(string todo_UnixTS)
//        {
//            BlazorCore.Services.SqlClient.ReadModel<TasksModel?> result = new();
//            try
//            {
//                // Parameter setzen
//                Dictionary<string, string> db_para = new()
//                {
//                    { "@Case_", "SelectByTodo_UnixTS>>Tasks" },
//                    { "@Todo_UnixTS", todo_UnixTS },
//                };
//                result = await _dam.ReadCompare<TasksModel>(db_para)!; // Abfrage ausführen
//                if (result != null && result.out_list != null) // Resultat prüfen
//                    return result ?? new BlazorCore.Services.SqlClient.ReadModel<TasksModel?>();
//            }
//            catch (Exception ex)
//            {
//                result.out_err = ex.Message;
//            }
//            return result ?? new BlazorCore.Services.SqlClient.ReadModel<TasksModel?>();
//        }
//    }
//}


//using Microsoft.JSInterop;
//using BlazorCore.Services.AppState;
//using BlazorCore.Services.LocalNotification;
//using BlazorCore.Services.Platform;
//using BlazorCore.Services.SqlClient;

//namespace pFemmeExample.Shared.Services.LocalNotification
//{
//    public class LocalNotification : ILocalNotification
//    {
//        private readonly IJSRuntime _js;
//        private readonly IPlatformBase _platform;
//        private readonly IAppStateBase _appState;

//        // Die beiden verschiedenen JS-Einstiegspunkte
//        // Native nutzt pE_Capacitor (cap.js), Web nutzt pE_Web (app.js)
//        private const string JsCapPrefix = "pE_Capacitor.notifications";
//        private const string JsWebPrefix = "pE_Web.notifications";

//        public LocalNotification(IJSRuntime js, IPlatformBase platformService, IAppStateBase appState)
//        {
//            _js = js;
//            _platform = platformService;
//            _appState = appState;
//        }

//        public string NotificationIdentifier { get; set; } = string.Empty;

//        private bool IsNative => _platform.GetCurrPlatform() != PLATFORMS.WASM;

//        // Hilfsmethode zur Bestimmung des Präfix basierend auf der Umgebung
//        private string GetPrefix() => IsNative ? JsCapPrefix : JsWebPrefix;

//        // Mapping für Methodennamen, da sie in app.js (Web) und cap.js (Native) variieren
//        private string GetMethodName(string nativeMethod, string webMethod) => IsNative ? nativeMethod : webMethod;

//        public Task InitializeAsync() => Task.CompletedTask;

//        public async Task<ScalarModel> RequestNotificationPermissionAsync()
//        {
//            var prefix = GetPrefix();
//            await _appState.Log($"[Notification] Requesting permission via {prefix}...");

//            try
//            {
//                // In beiden JS-Files heißt die Methode 'requestPermission'
//                return await _js.InvokeAsync<ScalarModel>($"{prefix}.requestPermission");
//            }
//            catch (Exception ex)
//            {
//                await _appState.Error($"[Notification] Permission Exception: {ex.Message}");
//                return new ScalarModel { out_err = ex.Message, out_value_bool = false };
//            }
//        }

//        public async Task<ScalarModel> ScheduleNotification(string identifier, string title, string body, DateTime? dateTime = null)
//        {
//            NotificationIdentifier = identifier;
//            long delay = 0;
//            if (dateTime.HasValue)
//            {
//                var diff = (long)(dateTime.Value - DateTime.Now).TotalMilliseconds;
//                delay = diff > 0 ? diff : 0;
//            }

//            var prefix = GetPrefix();
//            var method = GetMethodName("schedule", "scheduleNotification");

//            try
//            {
//                await _appState.Log($"[Notification] Scheduling via {prefix}.{method} (Delay: {delay}ms)");
//                return await _js.InvokeAsync<ScalarModel>($"{prefix}.{method}", identifier, title, body, delay);
//            }
//            catch (Exception ex)
//            {
//                await _appState.Error($"[Notification] Schedule Exception: {ex.Message}");
//                return new ScalarModel { out_err = ex.Message, out_value_bool = false };
//            }
//        }

//        public async Task<ScalarModel> RemovePendingNotification(string identifier)
//        {
//            var prefix = GetPrefix();
//            var method = GetMethodName("removePending", "removeScheduledNotification");

//            try
//            {
//                await _appState.Log($"[Notification] Removing pending via {prefix}.{method} (ID: {identifier})");
//                return await _js.InvokeAsync<ScalarModel>($"{prefix}.{method}", identifier);
//            }
//            catch (Exception ex)
//            {
//                await _appState.Error($"[Notification] RemovePending Exception: {ex.Message}");
//                return new ScalarModel { out_err = ex.Message };
//            }
//        }

//        public async Task<ScalarModel> RemoveAllPendingNotifications()
//        {
//            var prefix = GetPrefix();
//            var method = GetMethodName("removeAllPending", "removeAllScheduledNotifications");

//            try
//            {
//                await _appState.Log($"[Notification] Removing all pending via {prefix}.{method}");
//                return await _js.InvokeAsync<ScalarModel>($"{prefix}.{method}");
//            }
//            catch (Exception ex)
//            {
//                await _appState.Error($"[Notification] RemoveAllPending Exception: {ex.Message}");
//                return new ScalarModel { out_err = ex.Message };
//            }
//        }

//        public async Task<ScalarModel> RemoveDeliveredNotification(string identifier)
//        {
//            // 'Delivered' (Benachrichtigungs-Center) wird im Browser nicht unterstützt
//            if (!IsNative)
//            {
//                return new ScalarModel { out_value_bool = true, out_value_str = "Not supported in Web" };
//            }

//            try
//            {
//                return await _js.InvokeAsync<ScalarModel>($"{JsCapPrefix}.removeDelivered", identifier);
//            }
//            catch (Exception ex)
//            {
//                return new ScalarModel { out_err = ex.Message };
//            }
//        }

//        public async Task<ScalarModel> RemoveAllDeliveredNotifications()
//        {
//            if (!IsNative)
//            {
//                return new ScalarModel { out_value_bool = true, out_value_str = "Not supported in Web" };
//            }

//            try
//            {
//                return await _js.InvokeAsync<ScalarModel>($"{JsCapPrefix}.removeAllDelivered");
//            }
//            catch (Exception ex)
//            {
//                return new ScalarModel { out_err = ex.Message };
//            }
//        }

//        public async Task<ScalarModel> SyncNotificationsAsync(IEnumerable<NotificationSyncModel> cloudNotifications)
//        {
//            try
//            {
//                var prefix = GetPrefix();
//                var getIdsMethod = GetMethodName("getPendingIds", "getPendingIds");

//                // 1. Aktuelle IDs vom JS-Layer holen
//                var result = await _js.InvokeAsync<ScalarModel>($"{prefix}.{getIdsMethod}");
//                if (!result.out_value_bool) return result;

//                var localIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(result.out_value_str ?? "[]") ?? new List<string>();

//                // 2. Entfernen: Was lokal ist, aber nicht in der Cloud
//                foreach (var localId in localIds)
//                {
//                    if (!cloudNotifications.Any(c => c.Id == localId))
//                    {
//                        await RemovePendingNotification(localId);
//                    }
//                }

//                // 3. Hinzufügen: Was in der Cloud ist, aber nicht lokal
//                // (Bei Mobile/Web planen wir im Zweifel neu, wenn wir nicht sicher sind)
//                foreach (var cloud in cloudNotifications)
//                {
//                    if (!localIds.Contains(cloud.Id))
//                    {
//                        await ScheduleNotification(cloud.Id, cloud.Title, cloud.Body, cloud.TargetDate);
//                    }
//                    else if (IsNative)
//                    {
//                        // Native Optimierung: Da wir die Zeit nativ schwer vergleichen können, 
//                        // löschen wir es und planen es neu, um sicherzugehen (Update-Logik)
//                        await RemovePendingNotification(cloud.Id);
//                        await ScheduleNotification(cloud.Id, cloud.Title, cloud.Body, cloud.TargetDate);
//                    }
//                }

//                return new ScalarModel { out_value_bool = true };
//            }
//            catch (Exception ex)
//            {
//                await _appState.Error($"[Sync] Exception: {ex.Message}");
//                return new ScalarModel { out_err = ex.Message, out_value_bool = false };
//            }
//        }
//    }
//}
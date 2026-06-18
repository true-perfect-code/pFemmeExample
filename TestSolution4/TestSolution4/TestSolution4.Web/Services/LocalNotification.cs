using Microsoft.JSInterop;
using BlazorCore.Services.LocalNotification;
using BlazorCore.Services.SqlClient;

namespace TestSolution4.Web.Services
{
    public class LocalNotification : ILocalNotification
    {
        private readonly IJSRuntime _jsRuntime;
        // WICHTIG: Muss mit der Struktur in app.js übereinstimmen
        private const string JsPrefix = "pE_Web.notifications";

        public string NotificationIdentifier { get; set; } = string.Empty;

        public LocalNotification(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task<ScalarModel> RequestNotificationPermissionAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<ScalarModel>($"{JsPrefix}.requestPermission");
            }
            catch (Exception ex)
            {
                return new ScalarModel { out_err = ex.Message, out_value_bool = false };
            }
        }

        public async Task<ScalarModel> ScheduleNotification(string identifier, string title, string body, DateTime? dateTime = null)
        {
            try
            {
                // 1. Konvertierung des DateTime? in Unix-Sekunden (long)
                // Wir senden den absoluten Zielzeitpunkt, nicht die Verzögerung.
                long targetUnixSeconds = 0;

                if (dateTime.HasValue)
                {
                    // Umwandlung in UTC Unix Timestamp (Sekunden)
                    targetUnixSeconds = new DateTimeOffset(dateTime.Value).ToUnixTimeSeconds();
                }

                NotificationIdentifier = identifier;

                // 2. Übergabe des absoluten Zeitpunkts an JavaScript
                // JS berechnet dann: targetUnixSeconds - BrowserLocalTime
                return await _jsRuntime.InvokeAsync<ScalarModel>(
                    $"{JsPrefix}.scheduleNotification",
                    identifier,
                    title,
                    body,
                    targetUnixSeconds);
            }
            catch (Exception ex)
            {
                return new ScalarModel { out_err = ex.Message, out_value_bool = false };
            }
        }
        
        public async Task<ScalarModel> SyncNotificationsAsync(IEnumerable<NotificationSyncModel> cloudNotifications)
        {
            try
            {
                // 1. Aktuelle Timer-IDs aus dem Browser holen
                var result = await _jsRuntime.InvokeAsync<ScalarModel>($"{JsPrefix}.getPendingIds");
                if (!result.out_value_bool) return result;

                var localIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(result.out_value_str ?? "[]") ?? new List<string>();

                // 2. Veraltete Timer stoppen (Delta-Abgleich)
                // Alles was lokal geplant ist, aber nicht mehr in der Cloud-Liste steht
                foreach (var localId in localIds)
                {
                    if (!cloudNotifications.Any(c => c.Id == localId))
                    {
                        await RemovePendingNotification(localId);
                    }
                }

                // 3. Neue oder geänderte Timer setzen
                foreach (var cloud in cloudNotifications)
                {
                    // WICHTIG: Wir prüfen hier nur auf die ID. 
                    // Falls sich das Datum eines bestehenden Todos geändert hat, 
                    // sollte es in der cloudNotifications Liste mit neuer ID oder 
                    // nach einem Clear gelandet sein.
                    if (!localIds.Contains(cloud.Id))
                    {
                        // Hier rufen wir die neue Schedule-Logik auf.
                        // Wir übergeben das TargetDate (DateTime), welches intern in UnixTS umgewandelt wird.
                        await ScheduleNotification(cloud.Id, cloud.Title, cloud.Body, cloud.TargetDate);
                    }
                }

                return new ScalarModel { out_value_bool = true, out_value_str = "Sync completed" };
            }
            catch (Exception ex)
            {
                return new ScalarModel { out_err = ex.Message, out_value_bool = false };
            }
        }

        public async Task<ScalarModel> RemovePendingNotification(string identifier) =>
            await _jsRuntime.InvokeAsync<ScalarModel>($"{JsPrefix}.removeScheduledNotification", identifier);

        public async Task<ScalarModel> RemoveAllPendingNotifications() =>
            await _jsRuntime.InvokeAsync<ScalarModel>($"{JsPrefix}.removeAllScheduledNotifications");

        public Task<ScalarModel> RemoveDeliveredNotification(string identifier) =>
            Task.FromResult(new ScalarModel { out_value_bool = true, out_value_str = "Not supported in browser" });

        public Task<ScalarModel> RemoveAllDeliveredNotifications() =>
            Task.FromResult(new ScalarModel { out_value_bool = true, out_value_str = "Not supported in browser" });
    }
}
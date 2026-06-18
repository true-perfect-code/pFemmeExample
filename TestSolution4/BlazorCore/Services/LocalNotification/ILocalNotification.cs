using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.LocalNotification
{
    public interface ILocalNotification
    {
        string NotificationIdentifier { get; set; }

        Task InitializeAsync();

        Task<ScalarModel> RequestNotificationPermissionAsync();

        Task<ScalarModel> ScheduleNotification(string identifier, string title, string body, DateTime? dateTime = null);

        /// <summary>
        /// Synchronisiert lokale Benachrichtigungen basierend auf einem Delta-Vergleich.
        /// </summary>
        /// <param name="notifications">Die Liste der aktuell gültigen Benachrichtigungen (Cloud/DB-Stand).</param>
        Task<ScalarModel> SyncNotificationsAsync(IEnumerable<NotificationSyncModel> notifications);

        Task<ScalarModel> RemovePendingNotification(string identifier);
        Task<ScalarModel> RemoveAllPendingNotifications();
        Task<ScalarModel> RemoveDeliveredNotification(string identifier);
        Task<ScalarModel> RemoveAllDeliveredNotifications();
    }

    /// <summary>
    /// Einfaches Transport-Modell für den Sync-Prozess.
    /// </summary>
    public class NotificationSyncModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime TargetDate { get; set; }
    }
}
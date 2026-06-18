using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.Platform
{
    /// <summary>
    /// Plattform-unabhängiger Speicher für Secure Storage und Preferences.
    /// </summary>
    public interface IPlatformStorageBase
    {
        // ========================================================================
        // SECURE STORAGE (verschlüsselt, app-isoliert)
        // ========================================================================

        /// <summary>
        /// Speichert einen Wert verschlüsselt im plattformspezifischen Secure Storage.
        /// </summary>
        /// <param name="identifier">Eindeutiger Schlüssel</param>
        /// <param name="value">Zu speichernder Wert</param>
        /// <returns>ScalarModel mit out_value_bool = true bei Erfolg</returns>
        Task<ScalarModel> SetAsync(string identifier, string value);

        /// <summary>
        /// Liest einen Wert aus dem Secure Storage.
        /// </summary>
        /// <param name="identifier">Schlüssel</param>
        /// <returns>ScalarModel mit out_value_str = Wert, out_value_bool = true wenn gefunden</returns>
        Task<ScalarModel> GetAsync(string identifier);

        /// <summary>
        /// Entfernt einen Wert aus dem Secure Storage.
        /// </summary>
        /// <param name="identifier">Schlüssel</param>
        /// <returns>ScalarModel mit out_value_bool = true bei Erfolg</returns>
        Task<ScalarModel> RemoveAsync(string identifier);


        // ========================================================================
        // PREFERENCES (einfache Key-Value, unverschlüsselt)
        // ========================================================================

        /// <summary>
        /// Setzt einen Preference-Wert (plattformabhängig, nicht verschlüsselt).
        /// </summary>
        void SetPreference(string key, string value);

        /// <summary>
        /// Liest einen Preference-Wert.
        /// </summary>
        string? GetPreference(string key);

        /// <summary>
        /// Entfernt einen Preference-Wert.
        /// </summary>
        void RemovePreference(string key);

        /// <summary>
        /// Prüft, ob ein Preference-Key existiert.
        /// </summary>
        bool ContainsPreference(string key);
    }
}

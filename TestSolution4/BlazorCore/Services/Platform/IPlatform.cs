using BlazorCore.Services.Otp;
using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.Platform
{
    /// <summary>
    /// Provides platform-specific services, such as accessing the base directory of the application.
    /// </summary>
    public interface IPlatformBase
    {
        /// <summary>
        /// Gets the base directory of the application.
        /// </summary>
        /// <returns>
        /// A string representing the full path of the application's base directory.
        /// </returns>
        string GetBaseDirectory();

        ///// <summary>
        ///// Gets translations from xml-file for the application.
        ///// </summary>
        ///// <returns>
        ///// A Translations model representing all entries from xml-file.
        ///// </returns>
        //Task<Translations> GetTranslations();

        /// <summary>
        /// Gets current platform from xml-file for the application.
        /// </summary>
        /// <returns>
        /// A string representing current platform.
        /// </returns>
        PLATFORMS GetCurrPlatform();

        /// <summary>
        /// Gets current platform from xml-file for the application.
        /// </summary>
        /// <returns>
        /// A string representing current platform.
        /// </returns>
        p11.UI.NativeDevice GetCurrDevice();

        // ---

        //// Local storage
        ////Task<bool> SavingAllowed();
        //Task SetAsync(string identifier, string value);
        //Task RemoveAsync(string identifier);
        ////void RemoveAllAsync();
        ////string? Get(string identifier);
        //Task<string?> GetAsync(string identifier);
        Task<BlazorCore.Services.SqlClient.ScalarModel> SetAsync(string identifier, string value);
        Task<BlazorCore.Services.SqlClient.ScalarModel> RemoveAsync(string identifier);
        Task<BlazorCore.Services.SqlClient.ScalarModel> GetAsync(string identifier);

        // ---

        //// Utilities
        //void StartConnectivityMonitoring(AppState.IAppStateBase appState);
        //Task<bool> InternetConnectedAsync();

        string GetDeviceInfo();

        //string GetFormFactor();
        Task<string> GetFormFactor();

        Task<double> GetWindowWidth();
        Task<double> GetWindowHeight();

        Task<string> GetIdiomPlatform();

        Task CopyTextToClipboard(string text);


        // Otp 
        //Task<OtpModel> GenerateOtpAsync(string authUsers_UnixTS);
        Task<OtpModel> GenerateOtpAsync(OtpParametersModel otpParameters);

        // hiermit wird Endpoint in Blazor Server aufgerufen (wegen Pepper und Hashing)
        //Task<Authentication.OtpLoginStatus> CheckServerLoginState(Authentication.LoginModel login, bool isHashed = false);
        Task<bool> CheckServerLoginState(OtpParametersModel otpParameters);

        ////Task<ScalarModel> ValidateOtpCode(string account, string password, string userInputCode);
        //Task<ScalarModel> ValidateOtpCode(OtpParametersModel otpParameters, bool isHashed = false);

        //Task<ScalarModel> DeleteOtpKey(string username, string userpassword, string otpbackupcode);
        Task<ScalarModel> DeleteOtpKey(OtpParametersModel otpParameters, bool isHashed = false);


        // Idp
        /// <summary>
        /// Startet den Authentifizierungsfluss für die native Plattform, indem der externe Browser geöffnet wird.
        /// In diesem Polling-Ansatz wird hier kein JWT-Token direkt zurückgegeben oder auf ein Deep-Link-Ergebnis gewartet.
        /// </summary>
        /// <param name="authUrl">Die URL zu Ihrem Backend-Endpoint, der die Authentifizierung initiiert.</param>
        /// <returns>Ein Task, der abgeschlossen ist, sobald der Browser geöffnet wurde.</returns>
        Task<string?> AuthenticateAsync(string authUrl, bool openInNewTab = false);


        // Auswahl eines Verzeichnisses
        Task<string?> DirectoryPicker();

        // Auswahl einer Datei
        Task<ScalarModel> FilePicker(string Filter, string Title);


        // Sharing
        Task ShareText(string title, string text);


        // Feedback
        Task<BlazorCore.Services.SqlClient.ScalarModel> Feedback(Pages.ContactformModel ContactForm);

        // Open external url
        Task OpenExternalUrl(string url);

        Task<bool> IsTokenValidAsync(string token);



        //Preference
        //------------------------------
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


        Task<ScalarModel> InitializeJSAsync(); // Neu hinzufügen


        // Internetverbindung Status

        // File
        /// <summary>
        /// Speichert einen Datenstrom nativ auf der jeweiligen Plattform.
        /// Unter WPF wird System.IO genutzt, unter Capacitor das Filesystem-Plugin.
        /// </summary>
        /// <param name="filename">Der gewünschte Dateiname inkl. Erweiterung.</param>
        /// <param name="stream">Der MemoryStream mit den Dateidaten.</param>
        /// <param name="path">Der Zielpfad (wichtig für WPF, wird bei Capacitor ignoriert).</param>
        /// <returns>Ein ScalarModel, das über den Erfolg oder Fehler informiert.</returns>
        Task<BlazorCore.Services.SqlClient.ScalarModel> SaveFileNativeAsync(string filename, Stream stream, string? path = null, string title = "");


        // --- Navigation & Native Bridge ---

        /// <summary>
        /// Registriert die Brücke für native Navigations-Events (z.B. Android Back-Button).
        /// In der WASM-Implementierung übernimmt diese Methode auch das notwendige Delay.
        /// </summary>
        Task RegisterNativeNavigationAsync<T>(Microsoft.JSInterop.DotNetObjectReference<T> dotNetRef) where T : class;

        /// <summary>
        /// Steuert den Zustand der Swipe-Back Geste (primär für iOS/Web).
        /// </summary>
        Task SetSwipeBackStateAsync(bool enabled);

        /// <summary>
        /// Erzwingt einen Reset der Swipe-Geste (Cleanup nach Navigation).
        /// </summary>
        Task ForceResetSwipeAsync();

        /// <summary>
        /// Schließt die App oder das aktuelle Fenster (plattformabhängig).
        /// </summary>
        Task ExitAppAsync();

        ///// <summary>
        ///// Führt ein UI-Cleanup durch (z.B. Body-Scroll-Lock entfernen, Modale aufräumen).
        ///// </summary>
        //Task CleanupNativeUIAsync();

        Task NavigateBackAsync();

        // Log

        /// <summary>
        /// Schreibt eine Log-Nachricht auf das plattformspezifische Ziel (Datei, Konsole oder Speicher).
        /// </summary>
        void Log(string message, bool isError = false);

    }
}

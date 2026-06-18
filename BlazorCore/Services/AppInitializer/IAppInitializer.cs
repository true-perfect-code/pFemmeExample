using System.Reflection;

namespace BlazorCore.Services.AppInitializer
{
    public interface IAppInitializerBase
    {
        /// <summary>
        /// Performs the complete application initialization process asynchronously.
        /// This includes setting up all required states, loading configuration,
        /// initializing services, and preparing all necessary objects and variables
        /// for the application to function properly.
        /// </summary>
        /// <param name="assembly">The entry assembly for resource discovery</param>
        /// <param name="loadLanguages">If true, loads available language packs</param>
        /// <param name="loadTranslation">If true, loads translation dictionaries</param>
        /// <param name="initCss">If true, initializes CSS/theme system</param>
        /// <param name="initAccessibility">If true, initializes accessibility features</param>
        /// <returns>True if initialization succeeded, false otherwise</returns>
        Task<bool> AppInitializeAsync(
            Assembly assembly,
            bool loadLanguages = true,
            bool loadTranslation = true,
            bool initCss = true,
            bool initAccessibility = true);

        /// <summary>
        /// Called when user settings are saved. Re-initializes components that
        /// depend on dynamic configuration (e.g., theme, language, storage).
        /// </summary>
        Task InitializeAfterSavingAsync();

        /// <summary>
        /// Validates the current authentication token/session with the cloud API.
        /// </summary>
        /// <returns>True if authentication is still valid, false otherwise</returns>
        Task<bool> IsAuthenticationValid();

        /// <summary>
        /// Initializes platform-specific local notifications.
        /// On Web, this may register for Push API.
        /// On Native (WPF/Capacitor), this sets up the local notification service.
        /// </summary>
        Task<bool> InitializeLocalNotification();
    }
}
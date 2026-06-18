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
        /// <returns>
        /// A task that represents the asynchronous initialization operation.
        /// The task completes when all initialization steps are finished and
        /// the application is ready for normal operation.
        /// </returns>
        //Task<bool> InitializeAsync(Assembly assembly);
        Task<bool> AppInitializeAsync(
            Assembly assembly,
            bool LoadLanguages = true,
            bool LoadTranslation = true,
            bool InitCss = true,
            bool InitAccessibility = true);

        // Wenn Settings vom benutzer gespeichert sind, wird diese Methode aufgerufen.
        Task InitializeAfterSavingAsync();

        /// <summary>
        /// Gets a value indicating whether the application initialization process
        /// has been completed successfully and all required components are ready.
        /// </summary>
        /// <value>
        /// <c>true</c> if the initialization is complete and the application
        /// is ready to function; otherwise, <c>false</c>.
        /// </value>
        //bool IsInitialized { get; }

        Task<bool> IsAuthenticationValid();

        Task InitializeLocalNotification();
    }
}
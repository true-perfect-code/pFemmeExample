using Microsoft.Extensions.DependencyInjection;

namespace BlazorCore.Services.GlobalState
{
    /// <summary>
    /// Handles asynchronous initialization of the global state.
    /// Ensures that the global state is initialized exactly once, even in concurrent scenarios.
    /// </summary>
    public interface IGlobalStateInitializer
    {
        /// <summary>
        /// Initializes the global state asynchronously.
        /// This method is thread-safe and ensures initialization runs only once.
        /// </summary>
        Task InitializeAsync();
    }

    /// <summary>
    /// Default implementation of <see cref="IGlobalStateInitializer"/>.
    /// Uses a semaphore to guarantee thread-safe, one-time initialization.
    /// </summary>
    public sealed class GlobalStateInitializer : IGlobalStateInitializer
    {
        private readonly IServiceProvider _provider;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private bool _initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalStateInitializer"/> class.
        /// </summary>
        /// <param name="provider">Service provider for resolving <see cref="IGlobalStateBase"/>.</param>
        public GlobalStateInitializer(IServiceProvider provider)
        {
            _provider = provider;
        }

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            // Fast path: already initialized
            if (_initialized)
                return;

            await _lock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_initialized)
                    return;

                using var scope = _provider.CreateScope();
                var globalState = scope.ServiceProvider.GetRequiredService<IGlobalStateBase>();

                // Load translations, connection string, etc.
                await globalState.EnsureInitializedAsync();

                _initialized = true;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}


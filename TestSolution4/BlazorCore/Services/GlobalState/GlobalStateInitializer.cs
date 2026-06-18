using Microsoft.Extensions.DependencyInjection;

namespace BlazorCore.Services.GlobalState
{
    public interface IGlobalStateInitializer
    {
        Task InitializeAsync();
    }

    public sealed class GlobalStateInitializer : IGlobalStateInitializer
    {
        private readonly IServiceProvider _provider;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private bool _initialized;

        public GlobalStateInitializer(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            await _lock.WaitAsync();
            try
            {
                if (_initialized)
                    return;

                using var scope = _provider.CreateScope();

                var globalState = scope.ServiceProvider
                    .GetRequiredService<IGlobalStateBase>();

                // HIER: dein Grundbaustein (Sprachtabellen etc.)
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


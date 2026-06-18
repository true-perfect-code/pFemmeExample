using BlazorCore.Services.AppState;
using BlazorCore.Services.AI;
using BlazorCore.Services.SqlClient;
using BlazorCore.Services.GlobalState;

namespace pFemmeExample.Web.Services
{
    /// <summary>
    /// Central initializer for all core services that require async setup.
    /// Currently handles:
    /// - SQL Client (database connection)
    /// - AI Service (Azure OpenAI configuration)
    /// </summary>
    public class ServiceInitializer : IHostedService
    {
        private readonly IServiceProvider _provider;

        public ServiceInitializer(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _provider.CreateScope();

            // ========================================================================
            // 1. GLOBAL STATE INITIALIZATION
            // ========================================================================
            var globalState = scope.ServiceProvider.GetRequiredService<IGlobalStateBase>();
            globalState.GlobalInit(
                Shared.Global.Configuration.ConfigGeneral,
                Shared.Global.Configuration.ConfigWebapi,
                Shared.Global.Catalog.Sections
            );

            //// ========================================================================
            //// 2. SQL CLIENT INITIALIZATION (only if not LOCAL only)
            //// ========================================================================
            //if (Shared.Global.Configuration.ConfigGeneral.StorageLocation != STORAGE_LOCATION.LOCAL)
            //{
            //    var sqlClient = scope.ServiceProvider.GetService<ISqlClientBase>();
            //    if (sqlClient != null)
            //    {
            //        await sqlClient.InitializeAsync(globalState);
            //        if (sqlClient.isConnected)
            //            await sqlClient.MapApplTbls();
            //    }
            //}

            // ========================================================================
            // 3. AI SERVICE INITIALIZATION
            // ========================================================================
            var aiService = scope.ServiceProvider.GetService<IAI>();
            var aiConfig = scope.ServiceProvider.GetService<AIConfiguration>();

            if (aiService != null && aiConfig != null)
            {
                await aiService.ConfigInitializeAsync(aiConfig);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
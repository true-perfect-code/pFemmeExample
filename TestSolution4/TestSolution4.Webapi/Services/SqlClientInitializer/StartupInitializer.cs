namespace pWebApi.Services.StartupInitializer
{
    public class StartupInitializer : IHostedService
    {
        private readonly IServiceProvider _provider;

        public StartupInitializer(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _provider.CreateScope();
            var sqlClient = scope.ServiceProvider.GetRequiredService<BlazorCore.Services.SqlClient.ISqlClientBase>();

            await sqlClient.MapApplTbls();
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

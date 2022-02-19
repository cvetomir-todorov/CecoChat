using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Config.Snowflake;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Server.IDGen.HostedServices
{
    public sealed class InitDynamicConfig : IHostedService
    {
        private readonly ISnowflakeConfig _snowflakeConfig;

        public InitDynamicConfig(ISnowflakeConfig snowflakeConfig)
        {
            _snowflakeConfig = snowflakeConfig;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _snowflakeConfig.Initialize();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
﻿using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Config.History;
using CecoChat.Data.Config.Partitioning;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Connect.Server.Initialization
{
    public sealed class ConfigHostedService : IHostedService
    {
        private readonly IPartitioningConfig _partitioningConfig;
        private readonly IHistoryConfig _historyConfig;

        public ConfigHostedService(
            IPartitioningConfig partitioningConfig,
            IHistoryConfig historyConfig)
        {
            _partitioningConfig = partitioningConfig;
            _historyConfig = historyConfig;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _partitioningConfig.Initialize(new PartitioningConfigUsage
            {
                UseServerAddresses = true
            });
            await _historyConfig.Initialize(new HistoryConfigUsage
            {
                UseServerAddress = true
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

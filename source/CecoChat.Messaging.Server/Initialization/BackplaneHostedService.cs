using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Config.Partitioning;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Backplane;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Initialization
{
    public sealed class BackplaneHostedService : IHostedService, IDisposable
    {
        private readonly IBackplaneOptions _backplaneOptions;
        private readonly IBackplaneComponents _backplaneComponents;
        private readonly IPartitioningConfig _partitioningConfig;
        private readonly CancellationToken _appStoppingCt;
        private CancellationTokenSource _stoppedCts;

        public BackplaneHostedService(
            IHostApplicationLifetime applicationLifetime,
            IOptions<BackplaneOptions> backplaneOptions,
            IBackplaneComponents backplaneComponents,
            IPartitioningConfig partitioningConfig)
        {
            _backplaneOptions = backplaneOptions.Value;
            _backplaneComponents = backplaneComponents;
            _partitioningConfig = partitioningConfig;

            _appStoppingCt = applicationLifetime.ApplicationStopping;
        }

        public void Dispose()
        {
            _stoppedCts?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stoppedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _appStoppingCt);

            int partitionCount = _partitioningConfig.PartitionCount;
            PartitionRange partitions = _partitioningConfig.GetServerPartitions(_backplaneOptions.ServerID);

            _backplaneComponents.ConfigurePartitioning(partitionCount, partitions);
            _backplaneComponents.StartConsumption(_stoppedCts.Token);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

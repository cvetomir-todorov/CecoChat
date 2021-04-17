using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Configuration.Partitioning;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Backend;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Initialization
{
    public sealed class BackendHostedService : IHostedService, IDisposable
    {
        private readonly IBackendOptions _backendOptions;
        private readonly IBackendComponents _backendComponents;
        private readonly IPartitioningConfiguration _partitioningConfiguration;
        private readonly CancellationToken _appStoppingCt;
        private CancellationTokenSource _stoppedCts;

        public BackendHostedService(
            IHostApplicationLifetime applicationLifetime,
            IOptions<BackendOptions> backendOptions,
            IBackendComponents backendComponents,
            IPartitioningConfiguration partitioningConfiguration)
        {
            _backendOptions = backendOptions.Value;
            _backendComponents = backendComponents;
            _partitioningConfiguration = partitioningConfiguration;

            _appStoppingCt = applicationLifetime.ApplicationStopping;
        }

        public void Dispose()
        {
            _stoppedCts?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stoppedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _appStoppingCt);

            int partitionCount = _partitioningConfiguration.PartitionCount;
            PartitionRange partitions = _partitioningConfiguration.GetServerPartitions(_backendOptions.ServerID);

            _backendComponents.ConfigurePartitioning(partitionCount, partitions);
            _backendComponents.StartConsumption(_stoppedCts.Token);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

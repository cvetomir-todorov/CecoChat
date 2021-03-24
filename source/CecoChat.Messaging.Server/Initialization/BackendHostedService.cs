using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Configuration.Partitioning;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Backend;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Initialization
{
    public sealed class BackendHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IBackendComponents _backendComponents;
        private readonly IPartitioningConfiguration _partitioningConfiguration;

        public BackendHostedService(
            ILogger<BackendHostedService> logger,
            IOptions<BackendOptions> backendOptions,
            IBackendComponents backendComponents,
            IPartitioningConfiguration partitioningConfiguration)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _backendComponents = backendComponents;
            _partitioningConfiguration = partitioningConfiguration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            int partitionCount = _partitioningConfiguration.PartitionCount;
            PartitionRange partitions = _partitioningConfiguration.GetServerPartitions(_backendOptions.ServerID);

            _backendComponents.ConfigurePartitioning(partitionCount, partitions);
            StartBackendConsumer(_backendComponents.MessagesToReceiversConsumer, "messages to receivers", cancellationToken);
            StartBackendConsumer(_backendComponents.MessagesToSendersConsumer, "messages to senders", cancellationToken);

            return Task.CompletedTask;
        }

        private void StartBackendConsumer(IBackendConsumer consumer, string consumerID, CancellationToken ct)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    consumer.Start(ct);
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Failure in {0} consumer.", consumerID);
                }
            }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

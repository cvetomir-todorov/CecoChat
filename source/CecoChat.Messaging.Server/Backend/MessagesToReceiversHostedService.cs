using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Configuration.Messaging;
using CecoChat.Events;
using CecoChat.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public sealed class MessagesToReceiversHostedService : IHostedService, IDisposable, ISubscriber<PartitionsChangedEventData>
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IMessagingConfiguration _messagingConfiguration;
        private readonly IEvent<PartitionsChangedEventData> _partitionsChanged;
        private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
        private readonly IBackendConsumer _backendConsumer;

        private readonly Guid _partitionsChangedToken;

        public MessagesToReceiversHostedService(
            ILogger<MessagesToReceiversHostedService> logger,
            IOptions<BackendOptions> backendOptions,
            IMessagingConfiguration messagingConfiguration,
            IEvent<PartitionsChangedEventData> partitionsChanged,
            ITopicPartitionFlyweight topicPartitionFlyweight,
            IBackendConsumer backendConsumer)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _messagingConfiguration = messagingConfiguration;
            _partitionsChanged = partitionsChanged;
            _topicPartitionFlyweight = topicPartitionFlyweight;
            _backendConsumer = backendConsumer;

            _partitionsChangedToken = _partitionsChanged.Subscribe(this);
        }

        public void Dispose()
        {
            _partitionsChanged.Unsubscribe(_partitionsChangedToken);
        }

        public Task StartAsync(CancellationToken ct)
        {
            _topicPartitionFlyweight.Add(_backendOptions.MessagesTopicName, _messagingConfiguration.PartitionCount);
            _backendConsumer.Prepare();
            Task.Factory.StartNew(() =>
            {
                try
                {
                    _backendConsumer.Start(ct);
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Failure in send messages to receivers hosted service.");
                }
            }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);

            _logger.LogInformation("Started send messages to receivers hosted service.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public ValueTask Handle(PartitionsChangedEventData eventData)
        {
            _topicPartitionFlyweight.AddOrUpdate(_backendOptions.MessagesTopicName, _messagingConfiguration.PartitionCount);
            return ValueTask.CompletedTask;
        }
    }
}

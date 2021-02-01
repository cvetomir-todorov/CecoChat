using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public class MessagesToReceiversHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
        private readonly IBackendConsumer _backendConsumer;

        public MessagesToReceiversHostedService(
            ILogger<MessagesToReceiversHostedService> logger,
            IOptions<BackendOptions> backendOptions,
            ITopicPartitionFlyweight topicPartitionFlyweight,
            IBackendConsumer backendConsumer)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _topicPartitionFlyweight = topicPartitionFlyweight;
            _backendConsumer = backendConsumer;
        }

        public Task StartAsync(CancellationToken ct)
        {
            _topicPartitionFlyweight.Add(_backendOptions.MessagesTopicName, _backendOptions.MessagesTopicPartitionCount);
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
    }
}

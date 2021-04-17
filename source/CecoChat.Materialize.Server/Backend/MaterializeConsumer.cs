using System;
using System.Threading;
using CecoChat.Contracts.Backend;
using CecoChat.Data.History;
using CecoChat.Kafka;
using CecoChat.Server.Backend;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Materialize.Server.Backend
{
    public interface IMaterializeConsumer : IDisposable
    {
        void Prepare();

        void Start(CancellationToken ct);
    }

    public sealed class MaterializeConsumer : IMaterializeConsumer
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IKafkaConsumer<Null, BackendMessage> _consumer;
        private readonly INewMessageRepository _newMessageRepository;

        public MaterializeConsumer(
            ILogger<MaterializeConsumer> logger,
            IOptions<BackendOptions> backendOptions,
            IFactory<IKafkaConsumer<Null, BackendMessage>> consumerFactory,
            INewMessageRepository newMessageRepository)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _consumer = consumerFactory.Create();
            _newMessageRepository = newMessageRepository;
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }

        public void Prepare()
        {
            _consumer.Initialize(_backendOptions.Kafka, _backendOptions.MaterializeConsumer, new BackendMessageDeserializer());
            _consumer.Subscribe(_backendOptions.MessagesTopicName);
        }

        public void Start(CancellationToken ct)
        {
            _logger.LogInformation("Start materializing messages.");

            while (!ct.IsCancellationRequested)
            {
                if (_consumer.TryConsume(ct, out ConsumeResult<Null, BackendMessage> consumeResult))
                {
                    BackendMessage message = consumeResult.Message.Value;
                    _newMessageRepository.AddNewDialogMessage(message);
                    _consumer.Commit(consumeResult, ct);
                }
            }

            _logger.LogInformation("Stopped materializing messages.");
        }
    }
}

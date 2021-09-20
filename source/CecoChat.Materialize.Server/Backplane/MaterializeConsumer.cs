using System;
using System.Threading;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.History;
using CecoChat.Data.History;
using CecoChat.Kafka;
using CecoChat.Server;
using CecoChat.Server.Backplane;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Materialize.Server.Backplane
{
    public interface IMaterializeConsumer : IDisposable
    {
        void Prepare();

        void Start(CancellationToken ct);
    }

    public sealed class MaterializeConsumer : IMaterializeConsumer
    {
        private readonly ILogger _logger;
        private readonly IBackplaneOptions _backplaneOptions;
        private readonly IKafkaConsumer<Null, BackplaneMessage> _consumer;
        private readonly IMessageMapper _mapper;
        private readonly INewMessageRepository _newMessageRepository;

        public MaterializeConsumer(
            ILogger<MaterializeConsumer> logger,
            IOptions<BackplaneOptions> backplaneOptions,
            IFactory<IKafkaConsumer<Null, BackplaneMessage>> consumerFactory,
            IMessageMapper mapper,
            INewMessageRepository newMessageRepository)
        {
            _logger = logger;
            _backplaneOptions = backplaneOptions.Value;
            _consumer = consumerFactory.Create();
            _mapper = mapper;
            _newMessageRepository = newMessageRepository;
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }

        public void Prepare()
        {
            _consumer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.MaterializeConsumer, new BackplaneMessageDeserializer());
            _consumer.Subscribe(_backplaneOptions.MessagesTopicName);
        }

        public void Start(CancellationToken ct)
        {
            _logger.LogInformation("Start materializing messages.");

            while (!ct.IsCancellationRequested)
            {
                _consumer.Consume(consumeResult =>
                {
                    HistoryMessage historyMessage = _mapper.MapBackplaneToHistoryMessage(consumeResult.Message.Value);
                    _newMessageRepository.AddNewDialogMessage(historyMessage);
                }, ct);
            }

            _logger.LogInformation("Stopped materializing messages.");
        }
    }
}

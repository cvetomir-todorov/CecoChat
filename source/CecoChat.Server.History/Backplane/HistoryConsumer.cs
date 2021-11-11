using System;
using System.Threading;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.History;
using CecoChat.Data.History.Repos;
using CecoChat.Kafka;
using CecoChat.Server.Backplane;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.History.Backplane
{
    public interface IHistoryConsumer : IDisposable
    {
        void Prepare();

        void Start(CancellationToken ct);
    }

    public sealed class HistoryConsumer : IHistoryConsumer
    {
        private readonly ILogger _logger;
        private readonly BackplaneOptions _backplaneOptions;
        private readonly IKafkaConsumer<Null, BackplaneMessage> _consumer;
        private readonly IContractDataMapper _mapper;
        private readonly INewMessageRepo _newMessageRepo;
        private readonly IReactionRepo _reactionRepo;

        public HistoryConsumer(
            ILogger<HistoryConsumer> logger,
            IOptions<BackplaneOptions> backplaneOptions,
            IFactory<IKafkaConsumer<Null, BackplaneMessage>> consumerFactory,
            IContractDataMapper mapper,
            INewMessageRepo newMessageRepo,
            IReactionRepo reactionRepo)
        {
            _logger = logger;
            _backplaneOptions = backplaneOptions.Value;
            _consumer = consumerFactory.Create();
            _mapper = mapper;
            _newMessageRepo = newMessageRepo;
            _reactionRepo = reactionRepo;
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }

        public void Prepare()
        {
            _consumer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.HistoryConsumer, new BackplaneMessageDeserializer());
            _consumer.Subscribe(_backplaneOptions.TopicMessagesByReceiver);
        }

        public void Start(CancellationToken ct)
        {
            _logger.LogInformation("Start creating history from messages.");

            while (!ct.IsCancellationRequested)
            {
                _consumer.Consume(consumeResult =>
                {
                    Process(consumeResult.Message.Value);
                }, ct);
            }

            _logger.LogInformation("Stopped creating history from messages.");
        }

        private void Process(BackplaneMessage backplaneMessage)
        {
            switch (backplaneMessage.Type)
            {
                case MessageType.Data:
                    AddDataMessage(backplaneMessage);
                    break;
                case MessageType.Reaction:
                    AddReaction(backplaneMessage);
                    break;
                case MessageType.Disconnect:
                case MessageType.Delivery:
                    // ignore these
                    break;
                default:
                    throw new EnumValueNotSupportedException(backplaneMessage.Type);
            }
        }

        private void AddDataMessage(BackplaneMessage backplaneMessage)
        {
            DataMessage dataMessage = _mapper.CreateDataMessage(backplaneMessage);
            _newMessageRepo.AddMessage(dataMessage);
        }

        private void AddReaction(BackplaneMessage backplaneMessage)
        {
            ReactionMessage reactionMessage = _mapper.CreateReactionMessage(backplaneMessage);
            if (reactionMessage.Type == NewReactionType.Set)
            {
                _reactionRepo.SetReaction(reactionMessage);
            }
            else
            {
                _reactionRepo.UnsetReaction(reactionMessage);
            }
        }
    }
}

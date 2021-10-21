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
    public interface IMaterializeConsumer : IDisposable
    {
        void Prepare();

        void Start(CancellationToken ct);
    }

    public sealed class MaterializeConsumer : IMaterializeConsumer
    {
        private readonly ILogger _logger;
        private readonly BackplaneOptions _backplaneOptions;
        private readonly IKafkaConsumer<Null, BackplaneMessage> _consumer;
        private readonly IContractDataMapper _mapper;
        private readonly INewMessageRepo _newMessageRepo;
        private readonly IReactionRepo _reactionRepo;

        public MaterializeConsumer(
            ILogger<MaterializeConsumer> logger,
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
                    BackplaneMessage backplaneMessage = consumeResult.Message.Value;
                    switch (backplaneMessage.Type)
                    {
                        case MessageType.Data:
                            AddDataMessage(backplaneMessage);
                            break;
                        case MessageType.Reaction:
                            AddReaction(backplaneMessage);
                            break;
                        default:
                            throw new EnumValueNotSupportedException(backplaneMessage.Type);
                    }
                }, ct);
            }

            _logger.LogInformation("Stopped materializing messages.");
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

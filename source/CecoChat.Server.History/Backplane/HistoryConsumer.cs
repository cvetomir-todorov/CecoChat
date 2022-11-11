using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.History;
using CecoChat.Data.History.Repos;
using CecoChat.Kafka;
using CecoChat.Server.Backplane;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.History.Backplane;

public interface IHistoryConsumer : IDisposable
{
    void Prepare();

    void Start(CancellationToken ct);

    string ConsumerID { get; }
}

public sealed class HistoryConsumer : IHistoryConsumer
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly IKafkaConsumer<Null, BackplaneMessage> _consumer;
    private readonly IContractDataMapper _mapper;
    private readonly IChatMessageRepo _messageRepo;

    public HistoryConsumer(
        ILogger<HistoryConsumer> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        IFactory<IKafkaConsumer<Null, BackplaneMessage>> consumerFactory,
        IContractDataMapper mapper,
        IChatMessageRepo messageRepo)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _consumer = consumerFactory.Create();
        _mapper = mapper;
        _messageRepo = messageRepo;
    }

    public void Dispose()
    {
        _consumer.Dispose();
        _messageRepo.Dispose();
    }

    public void Prepare()
    {
        _consumer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.HistoryConsumer, new BackplaneMessageDeserializer());
        _consumer.Subscribe(_backplaneOptions.TopicMessagesByReceiver);
    }

    public void Start(CancellationToken ct)
    {
        _logger.LogInformation("Start creating history from messages");

        while (!ct.IsCancellationRequested)
        {
            _consumer.Consume(consumeResult =>
            {
                Process(consumeResult.Message.Value);
            }, ct);
        }

        _logger.LogInformation("Stopped creating history from messages");
    }

    public string ConsumerID => _backplaneOptions.HistoryConsumer.ConsumerGroupID;

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
        _messageRepo.AddMessage(dataMessage);
    }

    private void AddReaction(BackplaneMessage backplaneMessage)
    {
        ReactionMessage reactionMessage = _mapper.CreateReactionMessage(backplaneMessage);
        if (reactionMessage.Type == NewReactionType.Set)
        {
            _messageRepo.SetReaction(reactionMessage);
        }
        else
        {
            _messageRepo.UnsetReaction(reactionMessage);
        }
    }
}
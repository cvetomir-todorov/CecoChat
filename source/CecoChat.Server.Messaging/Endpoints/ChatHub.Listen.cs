using CecoChat.IdGen.Client;
using CecoChat.Messaging.Contracts;
using CecoChat.Server.Identity;
using CecoChat.Server.Messaging.Backplane;
using CecoChat.Server.Messaging.Clients;
using CecoChat.Server.Messaging.Telemetry;
using Microsoft.AspNetCore.SignalR;

namespace CecoChat.Server.Messaging.Endpoints;

public partial class ChatHub : Hub<IChatListener>, IChatHub
{
    private readonly ILogger _logger;
    private readonly IClientContainer _clientContainer;
    private readonly IMessagingTelemetry _messagingTelemetry;
    private readonly IContractMapper _mapper;
    private readonly IInputValidator _inputValidator;
    private readonly IIdGenClient _idGenClient;
    private readonly ISendersProducer _sendersProducer;

    public ChatHub(
        ILogger<ChatHub> logger,
        IClientContainer clientContainer,
        IMessagingTelemetry messagingTelemetry,
        IContractMapper mapper,
        IInputValidator inputValidator,
        IIdGenClient idGenClient,
        ISendersProducer sendersProducer)
    {
        _logger = logger;
        _clientContainer = clientContainer;
        _messagingTelemetry = messagingTelemetry;
        _mapper = mapper;
        _inputValidator = inputValidator;
        _idGenClient = idGenClient;
        _sendersProducer = sendersProducer;
    }

    public override async Task OnConnectedAsync()
    {
        UserClaims userClaims = Context.User!.GetUserClaimsSignalR(Context.ConnectionId, _logger, setUserIdTag: false);

        await _clientContainer.AddClient(userClaims.UserId, Context.ConnectionId);
        await base.OnConnectedAsync();
        _logger.LogTrace("Connect for user {UserId} with client {ClientId} with connection {ConnectionId}", userClaims.UserId, userClaims.ClientId, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        UserClaims userClaims = Context.User!.GetUserClaimsSignalR(Context.ConnectionId, _logger, setUserIdTag: false);

        await _clientContainer.RemoveClient(userClaims.UserId, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
        if (exception == null)
        {
            _logger.LogTrace("Disconnect for user {UserId} with client {ClientId} with connection {ConnectionId}", userClaims.UserId, userClaims.ClientId, Context.ConnectionId);
        }
        else
        {
            _logger.LogError(exception, "Disconnect for user {UserId} with client {ClientId} with connection {ConnectionId}", userClaims.UserId, userClaims.ClientId, Context.ConnectionId);
        }
    }
}

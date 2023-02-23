using CecoChat.Client.IDGen;
using CecoChat.Contracts.Messaging;
using CecoChat.Server.Identity;
using CecoChat.Server.Messaging.Backplane;
using CecoChat.Server.Messaging.Telemetry;
using Microsoft.AspNetCore.SignalR;

namespace CecoChat.Server.Messaging.Clients;

public partial class ChatHub : Hub<IChatListener>, IChatHub
{
    private readonly ILogger _logger;
    private readonly IClientContainer _clientContainer;
    private readonly IMessagingTelemetry _messagingTelemetry;
    private readonly IContractMapper _mapper;
    private readonly IInputValidator _inputValidator;
    private readonly IIDGenClient _idGenClient;
    private readonly ISendersProducer _sendersProducer;

    private const string MissingUserDataExMsg = "User has authenticated but data cannot be parsed from the access token.";

    public ChatHub(
        ILogger<ChatHub> logger,
        IClientContainer clientContainer,
        IMessagingTelemetry messagingTelemetry,
        IContractMapper mapper,
        IInputValidator inputValidator,
        IIDGenClient idGenClient,
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
        if (!Context.User!.TryGetUserClaims(out UserClaims? userClaims))
        {
            throw new HubException(MissingUserDataExMsg);
        }

        _messagingTelemetry.AddOnlineClient();
        _clientContainer.AddClient(userClaims.UserId);
        await Groups.AddToGroupAsync(Context.ConnectionId, _clientContainer.GetGroupName(userClaims.UserId));

        await base.OnConnectedAsync();
        _logger.LogInformation("Connect for user {UserId} with client {ClientId} with connection {ConnectionId}", userClaims.UserId, userClaims.ClientId, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (!Context.User!.TryGetUserClaims(out UserClaims? userClaims))
        {
            throw new HubException(MissingUserDataExMsg);
        }

        _messagingTelemetry.RemoveOnlineClient();
        _clientContainer.RemoveClient(userClaims.UserId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, _clientContainer.GetGroupName(userClaims.UserId));

        await base.OnDisconnectedAsync(exception);
        if (exception == null)
        {
            _logger.LogInformation("Disconnect for user {UserId} with client {ClientId} with connection {ConnectionId}", userClaims.UserId, userClaims.ClientId, Context.ConnectionId);
        }
        else
        {
            _logger.LogError(exception, "Disconnect for user {UserId} with client {ClientId} with connection {ConnectionId}", userClaims.UserId, userClaims.ClientId, Context.ConnectionId);
        }
    }
}

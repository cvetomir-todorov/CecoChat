using CecoChat.Contracts.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace Check.Connections.Server;

public class ChatHub : Hub<IChatListener>, IChatHub
{
    public Task<SendMessageResponse> SendMessage(SendMessageRequest request)
    {
        return Task.FromResult(new SendMessageResponse());
    }

    public Task<ReactResponse> React(ReactRequest request)
    {
        throw new NotSupportedException();
    }

    public Task<UnReactResponse> UnReact(UnReactRequest request)
    {
        throw new NotSupportedException();
    }
}

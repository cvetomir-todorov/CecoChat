using CecoChat.Messaging.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Check.Connections.Server;

public class ChatHub : Hub<IChatListener>, IChatHub
{
    public Task<SendPlainTextResponse> SendPlainText(SendPlainTextRequest request)
    {
        return Task.FromResult(new SendPlainTextResponse());
    }

    public Task<SendFileResponse> SendFile(SendFileRequest request)
    {
        throw new NotSupportedException();
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

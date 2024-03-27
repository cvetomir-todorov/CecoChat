using CecoChat.IdGen.Client;
using Microsoft.AspNetCore.SignalR;

namespace CecoChat.Server.Messaging.Endpoints;

public partial class ChatHub
{
    private async Task<long> GetMessageId()
    {
        // SignalR doesn't support yet CancellationToken in the method signature so we have none
        GetIdResult result = await _idGenClient.GetId(CancellationToken.None);
        if (!result.Success)
        {
            throw new HubException("Failed to obtain a message ID.");
        }

        return result.Id;
    }
}

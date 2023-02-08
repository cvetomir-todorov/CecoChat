using CecoChat.Http.Client;
using CecoChat.Polly;

namespace CecoChat.Client.User;

public class UserOptions
{
    public Uri? Address { get; set; }
    public TimeSpan CallTimeout { get; set; }

    public SocketsHttpHandlerOptions? SocketsHttpHandler { get; set; }
    public RetryOptions? Retry { get; set; }
}

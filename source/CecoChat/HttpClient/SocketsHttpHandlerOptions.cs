using System;

namespace CecoChat.HttpClient
{
    public sealed class SocketsHttpHandlerOptions
    {
        public TimeSpan KeepAlivePingDelay { get; set; }
        public TimeSpan KeepAlivePingTimeout { get; set; }
        public bool EnableMultipleHttp2Connections { get; set; }
    }
}
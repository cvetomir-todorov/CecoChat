using System;

namespace CecoChat.Messaging.Server.Identity
{
    public interface IIdentityOptions
    {
        Uri Address { get; }
    }

    public sealed class IdentityOptions : IIdentityOptions
    {
        public Uri Address { get; set; }
    }
}

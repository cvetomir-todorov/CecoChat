﻿using Cassandra;
using CecoChat.Cassandra;
using Microsoft.Extensions.Options;

namespace CecoChat.Data.History
{
    public interface ICecoChatDbContext : ICassandraDbContext
    {
        string MessagingKeyspace { get; }

        ISession Messaging { get; }
    }

    internal sealed class CecoChatDbContext : CassandraDbContext, ICecoChatDbContext
    {
        public CecoChatDbContext(IOptions<CassandraOptions> options) : base(options)
        {}

        public string MessagingKeyspace => "messaging";

        public ISession Messaging => GetSession(MessagingKeyspace);
    }
}

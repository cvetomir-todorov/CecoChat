using System;
using Cassandra;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.History.Repos
{
    internal interface IDataUtility
    {
        ISession MessagingSession { get; }

        PreparedStatement PrepareQuery(string cql);

        string CreateChatID(long userID1, long userID2);
    }

    internal sealed class DataUtility : IDataUtility
    {
        private readonly ILogger _logger;
        private readonly IHistoryDbContext _dbContext;

        public DataUtility(
            ILogger<DataUtility> logger,
            IHistoryDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public ISession MessagingSession => _dbContext.Messaging;

        public PreparedStatement PrepareQuery(string cql)
        {
            PreparedStatement preparedQuery = _dbContext.Messaging.Prepare(cql);
            _logger.LogDebug("Prepared CQL '{0}'.", cql);
            return preparedQuery;
        }

        public string CreateChatID(long userID1, long userID2)
        {
            long min = Math.Min(userID1, userID2);
            long max = Math.Max(userID1, userID2);

            return $"{min}-{max}";
        }
    }
}

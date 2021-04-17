using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.History;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CecoChat.Materialize.Server.Initialization
{
    public sealed class PrepareQueriesHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly INewMessageRepository _newMessageRepository;

        public PrepareQueriesHostedService(
            ILogger<PrepareQueriesHostedService> logger,
            INewMessageRepository newMessageRepository)
        {
            _logger = logger;
            _newMessageRepository = newMessageRepository;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Start preparing queries...");
                    _newMessageRepository.Prepare();
                    _logger.LogInformation("Completed preparing queries.");
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Failed to prepare queries.");
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

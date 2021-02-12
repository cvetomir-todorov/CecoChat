using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Messaging;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Materialize.Server.Initialization
{
    public sealed class PrepareQueriesHostedService : IHostedService
    {
        private readonly INewMessageRepository _newMessageRepository;

        public PrepareQueriesHostedService(
            INewMessageRepository newMessageRepository)
        {
            _newMessageRepository = newMessageRepository;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _newMessageRepository.Prepare();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

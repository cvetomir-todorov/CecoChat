using Microsoft.Extensions.Hosting;

namespace Common.AspNet.Init;

public class InitException : Exception
{
    public InitException() { }

    public InitException(string message) : base(message)
    { }

    public InitException(string message, Exception inner) : base(message, inner)
    { }
}

public sealed class InitHostedService : IHostedLifecycleService, IDisposable
{
    private readonly IEnumerable<InitStep> _initSteps;
    private CancellationTokenSource? _stopCts;

    public InitHostedService(IEnumerable<InitStep> initSteps)
    {
        _initSteps = initSteps;
    }

    public void Dispose()
    {
        _stopCts?.Dispose();
    }

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        _stopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        foreach (InitStep initStep in _initSteps)
        {
            bool success = await initStep.Execute(_stopCts.Token);
            if (!success)
            {
                throw new InitException();
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StoppedAsync(CancellationToken cancellationToken)
    {
        if (_stopCts != null)
        {
            await _stopCts.CancelAsync();
        }
    }
}

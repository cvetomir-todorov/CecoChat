using Microsoft.Extensions.Hosting;

namespace Common.AspNet.Init;

public abstract class InitStep : IDisposable
{
    protected InitStep(IHostApplicationLifetime applicationLifetime)
    {
        ApplicationStoppingCt = applicationLifetime.ApplicationStopping;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    { }

    protected CancellationToken ApplicationStoppingCt { get; }

    public Task<bool> Execute()
    {
        return DoExecute(ApplicationStoppingCt);
    }

    protected abstract Task<bool> DoExecute(CancellationToken ct);
}

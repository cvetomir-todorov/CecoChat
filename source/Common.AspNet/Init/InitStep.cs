namespace Common.AspNet.Init;

public abstract class InitStep : IDisposable
{
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    { }

    public abstract Task<bool> Execute(CancellationToken ct);
}

namespace CecoChat.Config;

internal interface IRepo<TValues>
    where TValues : class
{
    Task<TValues> Load(CancellationToken ct);
}

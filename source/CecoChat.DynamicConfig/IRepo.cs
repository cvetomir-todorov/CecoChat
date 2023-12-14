namespace CecoChat.DynamicConfig;

internal interface IRepo<TValues>
    where TValues: class
{
    Task<TValues> Load(CancellationToken ct);
}

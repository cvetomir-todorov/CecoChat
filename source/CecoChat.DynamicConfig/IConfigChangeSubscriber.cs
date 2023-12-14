namespace CecoChat.DynamicConfig;

public interface IConfigChangeSubscriber
{
    string ConfigSection { get; }

    Task NotifyConfigChange(CancellationToken ct);
}

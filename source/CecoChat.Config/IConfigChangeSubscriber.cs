namespace CecoChat.Config;

public interface IConfigChangeSubscriber
{
    string ConfigSection { get; }

    DateTime ConfigVersion { get; }

    Task NotifyConfigChange(CancellationToken ct);
}

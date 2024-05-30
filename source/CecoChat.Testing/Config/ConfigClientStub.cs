using CecoChat.Config.Client;
using CecoChat.Config.Contracts;

namespace CecoChat.Testing.Config;

public sealed class ConfigClientStub : IConfigClient
{
    private readonly ConfigElement[] _elements;

    public ConfigClientStub(ConfigElement[] elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        _elements = elements;
    }

    public void Dispose()
    { }

    public Task<IReadOnlyCollection<ConfigElement>> GetConfigElements(string configSection, CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyCollection<ConfigElement>>(_elements);
    }
}

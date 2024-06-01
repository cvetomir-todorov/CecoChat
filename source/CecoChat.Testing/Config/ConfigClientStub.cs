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

#pragma warning disable IDE0060 // Remove unused parameter
    public Task<IReadOnlyCollection<ConfigElement>> GetConfigElements(string configSection, CancellationToken ct)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        return Task.FromResult<IReadOnlyCollection<ConfigElement>>(_elements);
    }
}

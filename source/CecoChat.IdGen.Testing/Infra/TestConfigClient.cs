using CecoChat.Config;
using CecoChat.Config.Client;
using CecoChat.Config.Contracts;

namespace CecoChat.IdGen.Testing.Infra;

public class TestConfigClient : IConfigClient
{
    public void Dispose()
    { }

    public Task<IReadOnlyCollection<ConfigElement>> GetConfigElements(string configSection, CancellationToken ct)
    {
        ConfigElement[] elements =
        {
            new() { Name = ConfigKeys.Snowflake.GeneratorIds, Value = "123=0,1" }
        };

        return Task.FromResult<IReadOnlyCollection<ConfigElement>>(elements);
    }
}

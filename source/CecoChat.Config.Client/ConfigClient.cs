using CecoChat.Config.Contracts;
using Common;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Config.Client;

public interface IConfigClient : IDisposable
{
    Task<IReadOnlyCollection<ConfigElement>> GetConfigElements(string configSection, CancellationToken ct);
}

public sealed class ConfigClient : IConfigClient
{
    private readonly ILogger _logger;
    private readonly ConfigClientOptions _options;
    private readonly CecoChat.Config.Contracts.Config.ConfigClient _client;
    private readonly IClock _clock;

    public ConfigClient(
        ILogger<ConfigClient> logger,
        IOptions<ConfigClientOptions> options,
        CecoChat.Config.Contracts.Config.ConfigClient client,
        IClock clock)
    {
        _logger = logger;
        _options = options.Value;
        _client = client;
        _clock = clock;
    }

    public void Dispose()
    {
        // nothing to dispose for now, but keep the IDisposable as part of the contract
    }

    public async Task<IReadOnlyCollection<ConfigElement>> GetConfigElements(string configSection, CancellationToken ct)
    {
        GetConfigElementsRequest request = new()
        {
            ConfigSection = configSection
        };

        Metadata headers = new();
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        GetConfigElementsResponse response = await _client.GetConfigElementsAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received {ConfigElementCount} config elements for config section {ConfigSection}", response.Elements.Count, configSection);
        return response.Elements;
    }
}

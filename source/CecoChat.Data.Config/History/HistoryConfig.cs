using System.Diagnostics.CodeAnalysis;
using CecoChat.Data.Config.Common;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Config.History;

public interface IHistoryConfig
{
    Task<bool> Initialize();

    int MessageCount { get; }
}

internal sealed class HistoryConfig : IHistoryConfig
{
    private readonly ILogger _logger;
    private readonly IConfigSection<HistoryValues> _section;
    private HistoryValues? _values;

    public HistoryConfig(
        ILogger<HistoryConfig> logger,
        IConfigSection<HistoryValues> section)
    {
        _logger = logger;
        _section = section;
    }

    public int MessageCount
    {
        get
        {
            EnsureInitialized();
            return _values.MessageCount;
        }
    }

    public async Task<bool> Initialize()
    {
        InitializeResult<HistoryValues> result = await _section.Initialize(ConfigKeys.History.Section, PrintValues);
        _values = result.Values;

        return result.Success;
    }

    private void PrintValues(HistoryValues values)
    {
        _logger.LogInformation("Chat message count set to {MessageCount}", values.MessageCount);
    }

    [MemberNotNull(nameof(_values))]
    private void EnsureInitialized()
    {
        if (_values == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' to initialize the config.");
        }
    }
}

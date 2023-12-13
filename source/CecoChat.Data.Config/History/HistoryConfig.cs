﻿using CecoChat.Data.Config.Common;
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
            return _section.Values!.MessageCount;
        }
    }

    private void EnsureInitialized()
    {
        if (_section.Values == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' to initialize the config.");
        }
    }

    public async Task<bool> Initialize()
    {
        return await _section.Initialize(ConfigKeys.History.Section, PrintValues);
    }

    private void PrintValues(HistoryValues values)
    {
        _logger.LogInformation("Chat message count set to {MessageCount}", values.MessageCount);
    }
}

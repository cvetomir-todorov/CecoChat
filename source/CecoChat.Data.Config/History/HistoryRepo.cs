﻿using CecoChat.Data.Config.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Config.History;

internal sealed class HistoryRepo : IRepo<HistoryValues>
{
    private readonly ILogger _logger;
    private readonly ConfigDbContext _dbContext;

    public HistoryRepo(
        ILogger<HistoryRepo> logger,
        ConfigDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<HistoryValues> Load()
    {
        List<ElementEntity> elements = await _dbContext.Elements
            .Where(e => e.Name.StartsWith(ConfigKeys.History.Section))
            .AsNoTracking()
            .ToListAsync();

        HistoryValues values = new();

        ElementEntity? messageCount = elements.FirstOrDefault(e => string.Equals(e.Name, ConfigKeys.History.MessageCount, StringComparison.InvariantCultureIgnoreCase));
        if (messageCount != null)
        {
            values.MessageCount = ParseMessageCount(messageCount);
        }

        return values;
    }

    private int ParseMessageCount(ElementEntity element)
    {
        if (!int.TryParse(element.Value, out int messageCount))
        {
            _logger.LogError("Config {ConfigName} has invalid value '{ConfigValue}'", element.Name, element.Value);
        }

        return messageCount;
    }
}

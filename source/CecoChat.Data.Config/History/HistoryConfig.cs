using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Config.History;

internal sealed class HistoryConfig : IHistoryConfig
{
    private readonly ILogger _logger;
    private readonly IHistoryRepo _repo;
    private readonly IConfigUtility _configUtility;

    private HistoryValidator? _validator;
    private HistoryValues? _values;

    public HistoryConfig(
        ILogger<HistoryConfig> logger,
        IHistoryRepo repo,
        IConfigUtility configUtility)
    {
        _logger = logger;
        _repo = repo;
        _configUtility = configUtility;
    }

    public int MessageCount
    {
        get
        {
            EnsureInitialized();
            return _values!.MessageCount;
        }
    }

    public async Task<bool> Initialize()
    {
        try
        {
            _validator = new HistoryValidator();
            bool areValid = await LoadValidateValues();

            return areValid;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Initializing history configuration failed");
            return false;
        }
    }

    private async Task<bool> LoadValidateValues()
    {
        EnsureInitialized();

        HistoryValues values = await _repo.GetValues();
        _logger.LogInformation("Loading history configuration succeeded");

        bool areValid = _configUtility.ValidateValues("history", values, _validator!);
        if (areValid)
        {
            _values = values;
            PrintValues(values);
        }

        return areValid;
    }

    private void PrintValues(HistoryValues values)
    {
        _logger.LogInformation("Chat message count set to {MessageCount}", values.MessageCount);
    }

    private void EnsureInitialized()
    {
        if (_validator == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' to initialize the config.");
        }
    }
}

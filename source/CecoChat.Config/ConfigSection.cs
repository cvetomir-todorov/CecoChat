using System.Text;
using Common;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace CecoChat.Config;

internal interface IConfigSection<TValues>
    where TValues : class
{
    Task<bool> Initialize(string configContext, Action<TValues> printValues, CancellationToken ct);

    TValues? Values { get; }

    event EventHandler ValuesChanged;
}

internal sealed class ConfigSection<TValues> : IConfigSection<TValues>, IConfigChangeSubscriber
    where TValues : class
{
    private readonly ILogger _logger;
    private readonly IValidator<TValues> _validator;
    private readonly IRepo<TValues> _repo;
    private readonly IClock _clock;
    private string? _configContext;
    private Action<TValues>? _printValues;
    private DateTime _version;

    public ConfigSection(
        ILogger<ConfigSection<TValues>> logger,
        IValidator<TValues> validator,
        IRepo<TValues> repo,
        IClock clock)
    {
        _logger = logger;
        _validator = validator;
        _repo = repo;
        _clock = clock;
    }

    public async Task<bool> Initialize(string configContext, Action<TValues> printValues, CancellationToken ct)
    {
        try
        {
            _configContext = configContext;

            TValues values = await _repo.Load(ct);
            _logger.LogInformation("Loading {ConfigContext} configuration succeeded", _configContext);

            if (!ValidateValues(values))
            {
                return false;
            }

            Values = values;
            _version = _clock.GetNowUtc();

            _printValues = printValues;
            _printValues(values);

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Initializing {ConfigContext} configuration failed", configContext);
            return false;
        }
    }

    public TValues? Values { get; private set; }

    public event EventHandler? ValuesChanged;

    private bool ValidateValues(TValues values)
    {
        ValidationResult validationResult = _validator.Validate(values);
        if (validationResult.IsValid)
        {
            _logger.LogInformation("Validating {ConfigContext} configuration succeeded", _configContext);
        }
        else
        {
            StringBuilder errorBuilder = new();
            errorBuilder
                .AppendFormat("Validating {0} configuration failed.", _configContext)
                .AppendLine();

            foreach (ValidationFailure validationFailure in validationResult.Errors)
            {
                errorBuilder.AppendLine(validationFailure.ErrorMessage);
            }

            _logger.LogError("{ConfigValidationError}", errorBuilder.ToString());
        }

        return validationResult.IsValid;
    }

    string IConfigChangeSubscriber.ConfigSection => _configContext ?? string.Empty;

    public DateTime ConfigVersion => _version;

    public async Task NotifyConfigChange(CancellationToken ct)
    {
        TValues changedValues = await _repo.Load(ct);
        _logger.LogInformation("Loading changed {ConfigContext} configuration succeeded", _configContext);

        if (!ValidateValues(changedValues))
        {
            _logger.LogError("Using existing config for {ConfigContext}", _configContext);
        }

        _logger.LogInformation("Using new {ConfigContext} configuration", _configContext);

        Values = changedValues;
        _version = _clock.GetNowUtc();

        _printValues!(Values);

        ValuesChanged?.Invoke(this, EventArgs.Empty);
    }
}

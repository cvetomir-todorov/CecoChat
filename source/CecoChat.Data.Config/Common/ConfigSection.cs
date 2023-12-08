using System.Text;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Config.Common;

internal interface IConfigSection<TValues>
    where TValues: class
{
    Task<InitializeResult<TValues>> Initialize(string configContext, Action<TValues> printValues);
}

internal readonly struct InitializeResult<TValues>
    where TValues: class
{
    public TValues? Values { get; init; }

    public bool Success => Values != null;
}

internal sealed class ConfigSection<TValues> : IConfigSection<TValues>
    where TValues: class
{
    private readonly ILogger _logger;
    private readonly IValidator<TValues> _validator;
    private readonly IRepo<TValues> _repo;
    private string? _configContext;

    public ConfigSection(
        ILogger<ConfigSection<TValues>> logger,
        IValidator<TValues> validator,
        IRepo<TValues> repo)
    {
        _logger = logger;
        _validator = validator;
        _repo = repo;
    }
 
    public async Task<InitializeResult<TValues>> Initialize(string configContext, Action<TValues> printValues)
    {
        try
        {
            _configContext = configContext;

            TValues values = await _repo.Load();
            _logger.LogInformation("Loading {ConfigContext} configuration succeeded", _configContext);

            if (!ValidateValues(values))
            {
                return new InitializeResult<TValues>();
            }

            printValues(values);
            return new InitializeResult<TValues>
            {
                Values = values
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Initializing {ConfigContext} configuration failed", configContext);
            return new InitializeResult<TValues>();
        }
    }

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
}

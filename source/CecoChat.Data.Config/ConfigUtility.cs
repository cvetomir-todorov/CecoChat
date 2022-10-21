using System.Text;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Config;

internal interface IConfigUtility
{
    Task HandleChange(ChannelMessage channelMessage, Func<ChannelMessage, Task> handleAction);

    bool ValidateValues<TValues>(string configurationContext, TValues values, IValidator<TValues> validator);
}

internal sealed class ConfigUtility : IConfigUtility
{
    private readonly ILogger _logger;

    public ConfigUtility(
        ILogger<ConfigUtility> logger)
    {
        _logger = logger;
    }

    public async Task HandleChange(ChannelMessage channelMessage, Func<ChannelMessage, Task> handleAction)
    {
        try
        {
            _logger.LogInformation("Detected change {Change}", channelMessage);
            await handleAction(channelMessage);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while processing change {Change}", channelMessage);
        }
    }

    public bool ValidateValues<TValues>(string configurationContext, TValues values, IValidator<TValues> validator)
    {
        ValidationResult validationResult = validator.Validate(values);
        if (validationResult.IsValid)
        {
            _logger.LogInformation("Validating {ConfigContext} configuration succeeded", configurationContext);
        }
        else
        {
            StringBuilder errorBuilder = new();
            errorBuilder
                .AppendFormat("Validating {0} configuration failed.", configurationContext)
                .AppendLine();

            foreach (ValidationFailure validationFailure in validationResult.Errors)
            {
                errorBuilder.AppendLine(validationFailure.ErrorMessage);
            }

            _logger.LogError("{Error}", errorBuilder.ToString());
        }

        return validationResult.IsValid;
    }
}
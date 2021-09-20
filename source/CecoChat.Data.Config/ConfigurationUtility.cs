using System;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration
{
    internal interface IConfigurationUtility
    {
        Task HandleChange(ChannelMessage channelMessage, Func<ChannelMessage, Task> handleAction);

        bool ValidateValues<TValues>(string configurationContext, TValues values, IValidator<TValues> validator);
    }

    internal sealed class ConfigurationUtility : IConfigurationUtility
    {
        private readonly ILogger _logger;

        public ConfigurationUtility(
            ILogger<ConfigurationUtility> logger)
        {
            _logger = logger;
        }

        public async Task HandleChange(ChannelMessage channelMessage, Func<ChannelMessage, Task> handleAction)
        {
            try
            {
                _logger.LogInformation("Detected change {0}.", channelMessage);
                await handleAction(channelMessage);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error occurred while processing change {0}.", channelMessage);
            }
        }

        public bool ValidateValues<TValues>(string configurationContext, TValues values, IValidator<TValues> validator)
        {
            ValidationResult validationResult = validator.Validate(values);
            if (validationResult.IsValid)
            {
                _logger.LogInformation("Validating {0} configuration succeeded.", configurationContext);
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

                _logger.LogError(errorBuilder.ToString());
            }

            return validationResult.IsValid;
        }
    }
}

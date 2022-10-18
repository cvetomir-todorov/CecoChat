using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.Server.Bff.Controllers.Infrastructure;

public class DateTimeModelBinder : IModelBinder
{
    private static readonly string[] _dateTimeFormats =
    {
        "yyyyMMdd'T'HHmmss.FFFFFFFK", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
        "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm:ss.FFFFFFFK"
    };

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        string textValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
        if (bindingContext.ModelType == typeof(DateTime?) && string.IsNullOrEmpty(textValue))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        bool isParsed = DateTime.TryParseExact(textValue, _dateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime result);
        if (isParsed)
        {
            if (result.Kind == DateTimeKind.Local)
            {
                result = result.ToUniversalTime();
            }
            if (result.Kind == DateTimeKind.Unspecified)
            {
                result = new DateTime(result.Ticks, DateTimeKind.Utc);
            }
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        else
        {
            bindingContext.Result = ModelBindingResult.Failed();
        }

        return Task.CompletedTask;
    }
}
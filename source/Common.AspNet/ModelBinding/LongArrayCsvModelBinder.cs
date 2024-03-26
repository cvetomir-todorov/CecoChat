using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Common.AspNet.ModelBinding;

public class LongArrayCsvModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        if (bindingContext.ModelType != typeof(long[]))
        {
            throw new ArgumentException($"Model binder {GetType().FullName} should be used on type {typeof(long[])}.");
        }

        string? textValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
        if (string.IsNullOrEmpty(textValue))
        {
            bindingContext.Result = ModelBindingResult.Success(Array.Empty<long>());
            return Task.CompletedTask;
        }

        string[] stringValues = textValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        long[] values = new long[stringValues.Length];
        int invalidCount = 0;

        for (int i = 0; i < stringValues.Length; ++i)
        {
            if (!long.TryParse(stringValues[i], out long value))
            {
                invalidCount++;
            }
            else
            {
                values[i] = value;
            }
        }

        if (invalidCount > 0)
        {
            bindingContext.ModelState.AddModelError(bindingContext.FieldName, "Invalid long values in CSV list.");
            bindingContext.Result = ModelBindingResult.Failed();
        }
        else
        {
            bindingContext.Result = ModelBindingResult.Success(values);
        }

        return Task.CompletedTask;
    }
}

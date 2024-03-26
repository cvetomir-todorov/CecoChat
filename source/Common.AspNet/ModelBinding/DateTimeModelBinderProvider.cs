using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Common.AspNet.ModelBinding;

public class DateTimeModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Metadata.ModelType != typeof(DateTime) && context.Metadata.ModelType != typeof(DateTime?))
        {
            return null;
        }

        return new BinderTypeModelBinder(typeof(DateTimeModelBinder));
    }
}

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace CecoChat.Server.Bff.Controllers.Infrastructure;

public class DateTimeModelBinderProvider : IModelBinderProvider
{
    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        if (context.Metadata.ModelType != typeof(DateTime) && context.Metadata.ModelType != typeof(DateTime?))
        {
            return null;
        }

        return new BinderTypeModelBinder(typeof(DateTimeModelBinder));
    }
}
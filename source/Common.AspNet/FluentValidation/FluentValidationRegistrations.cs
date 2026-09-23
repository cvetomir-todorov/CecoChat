using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Common.AspNet.FluentValidation;

public static class FluentValidationRegistrations
{
    public static MvcOptions AddFluentValidationAutoValidation(this MvcOptions options)
    {
        options.Filters.Add<FluentValidationAutoValidationActionFilter>();

        return options;
    }

    public static IMvcBuilder DisableDataAnnotationsValidation(this IMvcBuilder builder)
    {
        return builder.ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);
    }
}

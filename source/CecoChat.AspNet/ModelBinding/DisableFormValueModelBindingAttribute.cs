using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CecoChat.AspNet.ModelBinding;

/// <summary>
/// Disables form model binding in order to avoid the request being buffered into memory.
/// </summary>
public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        context.ValueProviderFactories.RemoveType<FormValueProviderFactory>();
        context.ValueProviderFactories.RemoveType<FormFileValueProviderFactory>();
        context.ValueProviderFactories.RemoveType<JQueryFormValueProviderFactory>();
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    { }
}

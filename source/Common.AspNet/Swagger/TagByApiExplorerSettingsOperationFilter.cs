using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Common.AspNet.Swagger;

public sealed class TagByApiExplorerSettingsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
        {
            ApiExplorerSettingsAttribute? apiSettings = controllerActionDescriptor.ControllerTypeInfo
                .GetCustomAttributes(typeof(ApiExplorerSettingsAttribute))
                .Cast<ApiExplorerSettingsAttribute>()
                .FirstOrDefault();

            string groupName = controllerActionDescriptor.ControllerName;
            if (apiSettings != null && !string.IsNullOrWhiteSpace(apiSettings.GroupName))
            {
                groupName = apiSettings.GroupName;
            }

            OpenApiTag groupTag = new()
            {
                Name = groupName
            };
            operation.Tags = new List<OpenApiTag> { groupTag };
        }
    }
}

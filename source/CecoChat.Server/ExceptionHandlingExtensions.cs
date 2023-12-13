using System.Net.Mime;
using System.Text.Json;
using Calzolari.Grpc.Domain;
using Calzolari.Grpc.Net.Client.Validation;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CecoChat.Server;

public static class ExceptionHandlingExtensions
{
    public static void UseCustomExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(appError =>
        {
            appError.Run(async context =>
            {
                ProblemDetails details;
                IExceptionHandlerFeature? exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (exceptionFeature != null)
                {
                    if (exceptionFeature.Error is RpcException rpcException && rpcException.StatusCode == StatusCode.InvalidArgument)
                    {
                        details = ProcessInvalidArgumentRpcException(rpcException, context);
                    }
                    else
                    {
                        details = ProcessUnexpectedError(exceptionFeature.Error.Message, context);
                    }
                }
                else
                {
                    details = ProcessUnexpectedError("Unknown error", context);
                }

                await context.Response.WriteAsync(JsonSerializer.Serialize(details));
            });
        });
    }

    private static ProblemDetails ProcessInvalidArgumentRpcException(RpcException rpcException, HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = MediaTypeNames.Application.Json;

        ProblemDetails details = new()
        {
            Status = context.Response.StatusCode,
            Title = "Client error",
            Detail = rpcException.Message
        };

        foreach (ValidationTrailers failure in rpcException.GetValidationErrors())
        {
            if (details.Extensions.TryGetValue(failure.PropertyName, out object? value))
            {
                List<string> errorMessages = (List<string>)value!;
                errorMessages.Add(failure.ErrorMessage);
            }
            else
            {
                details.Extensions.Add(failure.PropertyName, new List<string> { failure.ErrorMessage });
            }
        }

        return details;
    }

    private static ProblemDetails ProcessUnexpectedError(string errorMessage, HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = MediaTypeNames.Application.Json;

        return new ProblemDetails
        {
            Status = context.Response.StatusCode,
            Title = "Unexpected error",
            Detail = errorMessage
        };
    }
}

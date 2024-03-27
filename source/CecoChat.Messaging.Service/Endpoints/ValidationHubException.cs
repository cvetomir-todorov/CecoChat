using FluentValidation.Results;
using Microsoft.AspNetCore.SignalR;

namespace CecoChat.Messaging.Service.Endpoints;

[Serializable]
public class ValidationHubException : HubException
{
    public ValidationHubException(List<ValidationFailure> errors)
        : base("Input validation failed.")
    {
        Errors = new List<ValidationFailure>(errors);
    }

    public List<ValidationFailure> Errors { get; }
}

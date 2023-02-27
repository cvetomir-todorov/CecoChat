using System.Runtime.Serialization;
using FluentValidation.Results;
using Microsoft.AspNetCore.SignalR;

namespace CecoChat.Server.Messaging.Clients;

[Serializable]
public class ValidationHubException : HubException
{
    public ValidationHubException(List<ValidationFailure> errors)
        : base("Input validation failed.")
    {
        Errors = new List<ValidationFailure>(errors);
    }

    protected ValidationHubException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        Errors = (List<ValidationFailure>)info.GetValue(nameof(Errors), typeof(List<ValidationFailure>))!;
    }

    public List<ValidationFailure> Errors { get; }
}

using System;
using FluentValidation;

namespace CecoChat.Server.Bff.Controllers
{
    public sealed class ConnectRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public sealed class ConnectRequestValidator : AbstractValidator<ConnectRequest>
    {
        public ConnectRequestValidator()
        {
            RuleFor(x => x.Username).NotNull().NotEmpty();
            RuleFor(x => x.Password).NotNull().NotEmpty();
        }
    }

    public sealed class ConnectResponse
    {
        public Guid ClientID { get; set; }
        public string AccessToken { get; set; }
    }
}
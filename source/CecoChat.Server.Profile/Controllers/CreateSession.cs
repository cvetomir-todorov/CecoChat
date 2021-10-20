using FluentValidation;

namespace CecoChat.Profile.Server.Controllers
{
    public sealed class CreateSessionRequest
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }

    public sealed class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
    {
        public CreateSessionRequestValidator()
        {
            RuleFor(x => x.Username).NotNull().NotEmpty();
            RuleFor(x => x.Password).NotNull().NotEmpty();
        }
    }

    public sealed class CreateSessionResponse
    {
        public string AccessToken { get; set; }
    }
}

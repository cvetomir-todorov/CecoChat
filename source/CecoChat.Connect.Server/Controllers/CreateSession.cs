using FluentValidation;

namespace CecoChat.Connect.Server.Controllers
{
    public sealed class CreateSessionRequest
    {
        public long UserID { get; set; }
    }

    public sealed class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
    {
        public CreateSessionRequestValidator()
        {
            RuleFor(x => x.UserID).GreaterThan(0);
        }
    }

    public sealed class CreateSessionResponse
    {
        public string MessagingServerAddress { get; set; }

        public string HistoryServerAddress { get; set; }
    }
}

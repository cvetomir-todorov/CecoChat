namespace CecoChat.Server.Identity;

public sealed class UserClaims
{
    public long UserId { get; init; }

    public Guid ClientId { get; init; }

    public override string ToString()
    {
        return $"{nameof(UserId)}:{UserId} {nameof(ClientId)}:{ClientId}";
    }
}

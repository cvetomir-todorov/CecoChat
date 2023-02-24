namespace CecoChat.Server.Identity;

public sealed class UserClaims
{
    public long UserId { get; set; }

    public Guid ClientId { get; set; }

    public override string ToString()
    {
        return $"{nameof(UserId)}:{UserId} {nameof(ClientId)}:{ClientId}";
    }
}

namespace CecoChat.Server.Bff.Auth;

public sealed class AuthOptions
{
    public bool ConsoleClientUsers { get; init; }

    public bool LoadTestingUsers { get; init; }

    public int LoadTestingUserCount { get; init; }
}

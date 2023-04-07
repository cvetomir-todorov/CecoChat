namespace CecoChat.Server.Bff.Endpoints;

public sealed class AuthOptions
{
    public bool ConsoleClientUsers { get; set; }

    public bool LoadTestingUsers { get; set; }

    public int LoadTestingUserCount { get; set; }
}

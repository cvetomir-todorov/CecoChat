using CecoChat.Chats.Contracts;
using FluentAssertions;
using NUnit.Framework;

namespace CecoChat.Chats.Testing.Tests;

public class GetUserChats : BaseTest
{
    [Test]
    public async Task AllCases()
    {
        string accessToken = CreateUserAccessToken(userId: 1331, userName: "lucky");
        IReadOnlyCollection<ChatState> userChats = await Client.Instance.GetUserChats(userId: 1, newerThan: DateTime.UtcNow, accessToken, CancellationToken.None);
        userChats.Should().BeEmpty();
    }
}

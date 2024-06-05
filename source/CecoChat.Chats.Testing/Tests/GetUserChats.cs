using CecoChat.Chats.Contracts;
using CecoChat.Chats.Data.Entities.UserChats;
using CecoChat.Data;
using Common;
using FluentAssertions;
using NUnit.Framework;

namespace CecoChat.Chats.Testing.Tests;

public class GetUserChats : BaseTest
{
    private const long UserBobby = 1331;
    private const long UserMaria = 1332;
    private const long UserPeter = 1333;
    private const long UserSofia = 1334;
    private const long UserGeorge = 1335;
    private const long UserWithoutAnyChats = 1336;
    private static readonly DateTime StartTime = DateTime.UtcNow;

    [OneTimeSetUp]
    public void AddTestData()
    {
        IUserChatsRepo userChatsRepo = Service.UserChats();

        // bobby
        AddChatsBetween(UserBobby, UserMaria, StartTime, userChatsRepo);
        AddChatsBetween(UserBobby, UserPeter, StartTime.AddMinutes(1), userChatsRepo);
        AddChatsBetween(UserBobby, UserSofia, StartTime.AddMinutes(2), userChatsRepo);
        AddChatsBetween(UserBobby, UserGeorge, StartTime.AddMinutes(3), userChatsRepo);

        // not bobby
        AddChatsBetween(UserMaria, UserSofia, StartTime.AddMinutes(1), userChatsRepo);
        AddChatsBetween(UserPeter, UserGeorge, StartTime.AddMinutes(2), userChatsRepo);
    }

    private static void AddChatsBetween(long userId, long otherUserId, DateTime newestMessage, IUserChatsRepo userChatsRepo)
    {
        userChatsRepo.UpdateUserChat(userId, CreateChatState(userId, otherUserId, newestMessage));
        userChatsRepo.UpdateUserChat(otherUserId, CreateChatState(otherUserId, userId, newestMessage));
    }

    private static ChatState CreateChatState(long userId, long otherUserId, DateTime newestMessage)
    {
        long newestMessageSnowflake = newestMessage.ToSnowflake();
        string chatId = DataUtility.CreateChatId(userId, otherUserId);

        return new ChatState
        {
            OtherUserId = otherUserId,
            ChatId = chatId,
            NewestMessage = newestMessageSnowflake,
            OtherUserDelivered = newestMessageSnowflake,
            OtherUserSeen = newestMessageSnowflake
        };
    }

    [OneTimeTearDown]
    public void CleanTestData()
    {
        // TODO: delete
    }

    public static object[] TestCases()
    {
        return
        [
            new object[]
            {
                "4 chats for bobby (all)", UserBobby, StartTime.AddSeconds(-1),
                new[]
                {
                    CreateChatState(UserBobby, UserMaria, StartTime),
                    CreateChatState(UserBobby, UserPeter, StartTime.AddMinutes(1)),
                    CreateChatState(UserBobby, UserSofia, StartTime.AddMinutes(2)),
                    CreateChatState(UserBobby, UserGeorge, StartTime.AddMinutes(3))
                }
            },
            new object[]
            {
                "3 chats for bobby", UserBobby, StartTime.AddMinutes(0.5),
                new[]
                {
                    CreateChatState(UserBobby, UserPeter, StartTime.AddMinutes(1)),
                    CreateChatState(UserBobby, UserSofia, StartTime.AddMinutes(2)),
                    CreateChatState(UserBobby, UserGeorge, StartTime.AddMinutes(3))
                }
            },
            new object[]
            {
                "2 chats for bobby", UserBobby, StartTime.AddMinutes(1.5),
                new[]
                {
                    CreateChatState(UserBobby, UserSofia, StartTime.AddMinutes(2)),
                    CreateChatState(UserBobby, UserGeorge, StartTime.AddMinutes(3))
                }
            },
            new object[]
            {
                "1 chat for bobby", UserBobby, StartTime.AddMinutes(2.5),
                new[]
                {
                    CreateChatState(UserBobby, UserGeorge, StartTime.AddMinutes(3))
                }
            },
            new object[]
            {
                "no chats for bobby", UserBobby, StartTime.AddMinutes(3.5),
                Array.Empty<ChatState>()
            },
            new object[]
            {
                "no chats for user without any chats", UserWithoutAnyChats, StartTime.AddSeconds(-1),
                Array.Empty<ChatState>()
            },
        ];
    }

    [TestCaseSource(nameof(TestCases))]
    public async Task AllCases(string testName, long userId, DateTime newerThan, ChatState[] expectedChats)
    {
        string accessToken = CreateUserAccessToken(userId, userName: "test");
        IReadOnlyCollection<ChatState> actualChats = await Client.Instance.GetUserChats(userId, newerThan, accessToken, CancellationToken.None);
        actualChats.Should().BeEquivalentTo(expectedChats, config => config.WithStrictOrdering());
    }
}

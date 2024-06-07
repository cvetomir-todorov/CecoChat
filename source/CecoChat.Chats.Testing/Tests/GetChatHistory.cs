using CecoChat.Chats.Contracts;
using CecoChat.Chats.Data.Entities.ChatMessages;
using Common;
using FluentAssertions;
using NUnit.Framework;

namespace CecoChat.Chats.Testing.Tests;

public class GetChatHistory : BaseTest
{
    private const long UserBobby = 1331;
    private const long UserMaria = 1332;
    private const long UserPeter = 1333;
    private static readonly DateTime StartTime = DateTime.UtcNow;
    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

    protected override Task CleanTestData()
    {
        IChatMessageRepo chatMessageRepo = Service.ChatMessages();

        chatMessageRepo.DeleteChat(UserBobby, UserMaria);
        chatMessageRepo.DeleteChat(UserBobby, UserPeter);

        return Task.CompletedTask;
    }

    protected override Task AddTestData()
    {
        IChatMessageRepo chatMessageRepo = Service.ChatMessages();

        // configured max message count is 4
        // timing and count
        AddPlainTextMessages(UserBobby, UserMaria, StartTime, OneMinute, count: 8, chatMessageRepo);
        AddPlainTextMessages(UserBobby, UserPeter, StartTime, OneMinute, count: 4, chatMessageRepo);

        return Task.CompletedTask;
    }

    private static void AddPlainTextMessages(long senderId, long receiverId, DateTime startTime, TimeSpan messageInterval, int count, IChatMessageRepo chatMessageRepo)
    {
        for (int i = 0; i < count; ++i)
        {
            DateTime timestamp = startTime.Add(messageInterval * i);

            PlainTextMessage message = new()
            {
                MessageId = timestamp.ToSnowflake(),
                SenderId = senderId,
                ReceiverId = receiverId,
                Text = $"text {i}"
            };

            chatMessageRepo.AddPlainTextMessage(message);
        }
    }

    private static HistoryMessage CreatePlainTextHistoryMessage(long senderId, long receiverId, DateTime timestamp, string text)
    {
        return new HistoryMessage
        {
            MessageId = timestamp.ToSnowflake(),
            SenderId = senderId,
            ReceiverId = receiverId,
            DataType = DataType.PlainText,
            Text = text
        };
    }

    public static object[] MessagesOlderThanAndLimitedInCountTestCases()
    {
        return
        [
            new object[]
            {
                "oldest 2", UserBobby, UserMaria, StartTime.AddMinutes(1.1),
                new[]
                {
                    CreatePlainTextHistoryMessage(UserBobby, UserMaria, StartTime, "text 0"),
                    CreatePlainTextHistoryMessage(UserBobby, UserMaria, StartTime.AddMinutes(1), "text 1"),
                }
            },
            new object[]
            {
                "newest 4", UserBobby, UserMaria, StartTime.AddMinutes(7.1),
                new[]
                {
                    CreatePlainTextHistoryMessage(UserBobby, UserMaria, StartTime.AddMinutes(4), "text 4"),
                    CreatePlainTextHistoryMessage(UserBobby, UserMaria, StartTime.AddMinutes(5), "text 5"),
                    CreatePlainTextHistoryMessage(UserBobby, UserMaria, StartTime.AddMinutes(6), "text 6"),
                    CreatePlainTextHistoryMessage(UserBobby, UserMaria, StartTime.AddMinutes(7), "text 7"),
                }
            },
            new object[]
            {
                "between 2 and 6", UserBobby, UserMaria, StartTime.AddMinutes(5.1),
                new[]
                {
                    CreatePlainTextHistoryMessage(UserBobby, UserMaria, StartTime.AddMinutes(2), "text 2"),
                    CreatePlainTextHistoryMessage(UserBobby, UserMaria, StartTime.AddMinutes(3), "text 3"),
                    CreatePlainTextHistoryMessage(UserBobby, UserMaria, StartTime.AddMinutes(4), "text 4"),
                    CreatePlainTextHistoryMessage(UserBobby, UserMaria, StartTime.AddMinutes(5), "text 5"),
                }
            },
            new object[]
            {
                "the only 4", UserBobby, UserPeter, StartTime.AddMinutes(120),
                new[]
                {
                    CreatePlainTextHistoryMessage(UserBobby, UserPeter, StartTime, "text 0"),
                    CreatePlainTextHistoryMessage(UserBobby, UserPeter, StartTime.AddMinutes(1), "text 1"),
                    CreatePlainTextHistoryMessage(UserBobby, UserPeter, StartTime.AddMinutes(2), "text 2"),
                    CreatePlainTextHistoryMessage(UserBobby, UserPeter, StartTime.AddMinutes(3), "text 3"),
                }
            }
        ];
    }

    [TestCaseSource(nameof(MessagesOlderThanAndLimitedInCountTestCases))]
    public async Task MessagesOlderThanAndLimitedInCount(string testName, long userId, long otherUserId, DateTime olderThan, HistoryMessage[] expectedMessages)
    {
        string accessToken = CreateUserAccessToken(userId, "test");
        IReadOnlyCollection<HistoryMessage> actualMessages = await Client.Instance.GetChatHistory(userId, otherUserId, olderThan, accessToken, CancellationToken.None);
        actualMessages.Should().BeEquivalentTo(expectedMessages, config => config.Including(x => x.Text));
    }
}

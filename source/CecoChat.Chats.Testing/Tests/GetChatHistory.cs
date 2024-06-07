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
    private const long UserSofia = 1334;
    private const long UserGeorge = 1335;
    private const long UserHelen = 1336;
    private static readonly DateTime StartTime = DateTime.UtcNow;
    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

    protected override Task CleanTestData()
    {
        IChatMessageRepo chatMessageRepo = Service.ChatMessages();

        chatMessageRepo.DeleteChat(UserBobby, UserMaria);
        chatMessageRepo.DeleteChat(UserBobby, UserPeter);
        chatMessageRepo.DeleteChat(UserBobby, UserSofia);
        chatMessageRepo.DeleteChat(UserBobby, UserGeorge);
        chatMessageRepo.DeleteChat(UserBobby, UserHelen);

        return Task.CompletedTask;
    }

    protected override Task AddTestData()
    {
        IChatMessageRepo chatMessageRepo = Service.ChatMessages();

        // configured max message count is 4
        // timing and count
        AddPlainTextMessages(UserBobby, UserMaria, StartTime, OneMinute, count: 8, chatMessageRepo);
        AddPlainTextMessages(UserBobby, UserPeter, StartTime, OneMinute, count: 3, chatMessageRepo);

        // all types of messages
        chatMessageRepo.AddPlainTextMessage(new PlainTextMessage
        {
            MessageId = StartTime.ToSnowflake(),
            SenderId = UserBobby,
            ReceiverId = UserSofia,
            Text = "plain text"
        });
        chatMessageRepo.AddFileMessage(new FileMessage
        {
            MessageId = StartTime.ToSnowflake(),
            SenderId = UserBobby,
            ReceiverId = UserGeorge,
            Text = "file",
            Bucket = "test bucket",
            Path = "test path"
        });
        chatMessageRepo.AddPlainTextMessage(new PlainTextMessage
        {
            MessageId = StartTime.ToSnowflake(),
            SenderId = UserBobby,
            ReceiverId = UserHelen,
            Text = "reactions"
        });
        chatMessageRepo.SetReaction(new ReactionMessage
        {
            MessageId = StartTime.ToSnowflake(),
            SenderId = UserBobby,
            ReceiverId = UserHelen,
            Type = NewReactionType.Set,
            ReactorId = UserBobby,
            Reaction = "thumbs-up"
        });
        chatMessageRepo.SetReaction(new ReactionMessage
        {
            MessageId = StartTime.ToSnowflake(),
            SenderId = UserBobby,
            ReceiverId = UserHelen,
            Type = NewReactionType.Set,
            ReactorId = UserHelen,
            Reaction = "sad-face"
        });

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

    [TestCaseSource(nameof(MessagesOlderThanAndLimitedInCountTestCases))]
    public async Task MessagesOlderThanAndLimitedInCount(string testName, long userId, long otherUserId, DateTime olderThan, HistoryMessage[] expectedMessages)
    {
        string accessToken = CreateUserAccessToken(userId, "test");
        IReadOnlyCollection<HistoryMessage> actualMessages = await Client.Instance.GetChatHistory(userId, otherUserId, olderThan, accessToken, CancellationToken.None);
        actualMessages.Should().BeEquivalentTo(expectedMessages, config => config.Including(x => x.Text));
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
                "the only 3", UserBobby, UserPeter, StartTime.AddMinutes(120),
                new[]
                {
                    CreatePlainTextHistoryMessage(UserBobby, UserPeter, StartTime, "text 0"),
                    CreatePlainTextHistoryMessage(UserBobby, UserPeter, StartTime.AddMinutes(1), "text 1"),
                    CreatePlainTextHistoryMessage(UserBobby, UserPeter, StartTime.AddMinutes(2), "text 2"),
                }
            }
        ];
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

    [TestCaseSource(nameof(AllTypesOfMessagesTestCases))]
    public async Task AllTypesOfMessages(string testName, long userId, long otherUserId, HistoryMessage expectedMessage)
    {
        string accessToken = CreateUserAccessToken(userId, "test");
        IReadOnlyCollection<HistoryMessage> actualMessages = await Client.Instance.GetChatHistory(userId, otherUserId, olderThan: DateTime.UtcNow, accessToken, CancellationToken.None);

        actualMessages.Count.Should().Be(1);
        actualMessages.First().Should().Be(expectedMessage);
    }

    public static object[] AllTypesOfMessagesTestCases()
    {
        return
        [
            new object[]
            {
                "plain text message", UserBobby, UserSofia,
                CreateHistoryMessage(UserBobby, UserSofia, StartTime, "plain text", DataType.PlainText)
            },
            new object[]
            {
                "file message", UserBobby, UserGeorge,
                CreateHistoryMessage(UserBobby, UserGeorge, StartTime, "file", DataType.File, new HistoryFileData
                {
                    Bucket = "test bucket", Path = "test path"
                })
            },
            new object[]
            {
                "message with reactions", UserBobby, UserHelen,
                CreateHistoryMessage(UserBobby, UserHelen, StartTime, "reactions", DataType.PlainText, reactions: new Dictionary<long, string>
                {
                    [UserBobby] = "thumbs-up",
                    [UserHelen] = "sad-face"
                })
            },
        ];
    }

    private static HistoryMessage CreateHistoryMessage(
        long senderId, long receiverId, DateTime timestamp, string text, DataType type,
        HistoryFileData? fileData = null, Dictionary<long, string>? reactions = null)
    {
        HistoryMessage message = new()
        {
            MessageId = timestamp.ToSnowflake(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Text = text,
            DataType = type
        };

        if (fileData != null)
        {
            message.File = fileData;
        }
        if (reactions != null)
        {
            message.Reactions.Add(reactions);
        }

        return message;
    }
}

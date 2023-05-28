using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace CecoChat.ConsoleClient.LocalStorage;

public sealed class Chat
{
    private readonly ConcurrentDictionary<long, Message> _messageMap;

    public Chat(long otherUserId)
    {
        OtherUserId = otherUserId;
        _messageMap = new();
    }

    public long OtherUserId { get; }

    // The ID of the newest message in the chat.
    public long NewestMessage { get; set; }

    // The ID of the last processed message. 
    public long Processed { get; set; }

    // The ID of the last message delivered to the other user.
    public long OtherUserDelivered { get; set; }

    // The ID of the last message seen by the other user.
    public long OtherUserSeen { get; set; }

    public void AddNew(Message message)
    {
        if (!_messageMap.TryGetValue(message.MessageId, out _))
        {
            _messageMap.TryAdd(message.MessageId, message);
        }
    }

    public IEnumerable<Message> GetMessages()
    {
        foreach (KeyValuePair<long, Message> pair in _messageMap)
        {
            yield return pair.Value;
        }
    }

    public bool TryGetMessage(long messageId, [NotNullWhen(returnValue: true)] out Message? message)
    {
        return _messageMap.TryGetValue(messageId, out message);
    }
}

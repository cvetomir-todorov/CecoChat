using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace CecoChat.ConsoleClient.LocalStorage;

public sealed class MessageStorage
{
    private readonly long _userId;
    private readonly ConcurrentDictionary<long, Chat> _chatMap;

    public MessageStorage(long userId)
    {
        _userId = userId;
        _chatMap = new();
    }

    public void AddOrUpdateChat(Chat chat)
    {
        Chat targetChat = _chatMap.GetOrAdd(chat.OtherUserId, _ => chat);
        if (!ReferenceEquals(chat, targetChat))
        {
            targetChat.NewestMessage = chat.NewestMessage;
            targetChat.OtherUserDelivered = chat.OtherUserDelivered;
            targetChat.OtherUserSeen = chat.OtherUserSeen;
        }
    }

    public void AddMessage(Message message)
    {
        long otherUserId = GetOtherUserId(message.SenderId, message.ReceiverId);
        Chat chat = _chatMap.GetOrAdd(otherUserId, _ => new Chat(otherUserId));
        chat.AddNew(message);
    }

    public List<long> GetUsers()
    {
        return _chatMap.Keys.ToList();
    }

    public List<Message> GetChatMessages(long userId)
    {
        List<Message> messages = new();

        if (_chatMap.TryGetValue(userId, out Chat? chat))
        {
            messages.AddRange(chat.GetMessages());
        }

        return messages;
    }

    public bool TryGetMessage(long userId1, long userId2, long messageId, [NotNullWhen(returnValue: true)] out Message? message)
    {
        long otherUserId = GetOtherUserId(userId1, userId2);

        message = null;
        if (!_chatMap.TryGetValue(otherUserId, out Chat? dialog))
        {
            return false;
        }

        return dialog.TryGetMessage(messageId, out message);
    }

    public bool TryGetChat(long userId1, long userId2, [NotNullWhen(returnValue: true)] out Chat? chat)
    {
        long otherUserId = GetOtherUserId(userId1, userId2);
        return _chatMap.TryGetValue(otherUserId, out chat);
    }

    public long GetOtherUserId(long userId1, long userId2)
    {
        if (userId1 != _userId)
        {
            return userId1;
        }
        if (userId2 != _userId)
        {
            return userId2;
        }

        throw new InvalidOperationException($"Message is from current user {_userId} to himself.");
    }
}

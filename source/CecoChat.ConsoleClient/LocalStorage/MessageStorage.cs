using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CecoChat.ConsoleClient.LocalStorage;

public sealed class MessageStorage
{
    private readonly long _userID;
    private readonly ConcurrentDictionary<long, Chat> _chatMap;

    public MessageStorage(long userID)
    {
        _userID = userID;
        _chatMap = new();
    }

    public void AddOrUpdateChat(Chat chat)
    {
        Chat targetChat = _chatMap.GetOrAdd(chat.OtherUserID, _ => chat);
        if (!ReferenceEquals(chat, targetChat))
        {
            targetChat.NewestMessage = chat.NewestMessage;
            targetChat.OtherUserDelivered = chat.OtherUserDelivered;
            targetChat.OtherUserSeen = chat.OtherUserSeen;
        }
    }

    public void AddMessage(Message message)
    {
        long otherUserId = GetOtherUserID(message.SenderID, message.ReceiverID);
        Chat chat = _chatMap.GetOrAdd(otherUserId, _ => new Chat(otherUserId));
        chat.AddNew(message);
    }

    public List<long> GetUsers()
    {
        return _chatMap.Keys.ToList();
    }

    public List<Message> GetChatMessages(long userID)
    {
        List<Message> messages = new();

        if (_chatMap.TryGetValue(userID, out Chat chat))
        {
            messages.AddRange(chat.GetMessages());
        }

        return messages;
    }

    public bool TryGetMessage(long userID1, long userID2, long messageID, out Message message)
    {
        long otherUserID = GetOtherUserID(userID1, userID2);

        message = null;
        if (!_chatMap.TryGetValue(otherUserID, out Chat dialog))
        {
            return false;
        }

        return dialog.TryGetMessage(messageID, out message);
    }

    public bool TryGetChat(long userID1, long userID2, out Chat chat)
    {
        long otherUserID = GetOtherUserID(userID1, userID2);
        return _chatMap.TryGetValue(otherUserID, out chat);
    }

    public long GetOtherUserID(long userID1, long userID2)
    {
        if (userID1 != _userID)
        {
            return userID1;
        }
        if (userID2 != _userID)
        {
            return userID2;
        }

        throw new InvalidOperationException($"Message is from current user {_userID} to himself.");
    }
}
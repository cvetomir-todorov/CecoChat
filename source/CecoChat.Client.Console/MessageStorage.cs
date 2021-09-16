using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CecoChat.Contracts.Client;

namespace CecoChat.Client.Console
{
    public sealed class Message
    {
        public ClientMessage Data { get; init; }
        public int SequenceNumber { get; init; }
        public string AckStatus { get; set; }
    }

    public sealed class MessageStorage
    {
        private readonly long _userID;
        private readonly ConcurrentDictionary<long, Dialog> _dialogMap;

        public MessageStorage(long userID)
        {
            _userID = userID;
            _dialogMap = new();
        }

        public void AddMessage(ListenResponse response)
        {
            long otherUserId = GetOtherUserID(response.Message);
            Dialog dialog = _dialogMap.GetOrAdd(otherUserId, _ => new Dialog());
            dialog.AddOrAcknowledge(response);
        }

        public void AcknowledgeMessage(ListenResponse response)
        {
            long otherUserID = GetOtherUserID(response.Message);
            Dialog dialog = _dialogMap.GetOrAdd(otherUserID, _ => new Dialog());
            dialog.AddOrAcknowledge(response);
        }

        public List<long> GetUsers()
        {
            return _dialogMap.Keys.ToList();
        }

        public List<Message> GetDialogMessages(long userID)
        {
            List<Message> messages = new();

            if (_dialogMap.TryGetValue(userID, out Dialog dialog))
            {
                messages.AddRange(dialog.GetMessages());
            }

            return messages;
        }

        private long GetOtherUserID(ClientMessage message)
        {
            if (message.SenderId != _userID)
            {
                return message.SenderId;
            }
            if (message.ReceiverId != _userID)
            {
                return message.ReceiverId;
            }

            throw new InvalidOperationException($"Message '{message}' is from current user {_userID} to himself.");
        }

        private sealed class Dialog
        {
            private readonly ConcurrentDictionary<long, Message> _messageMap;

            public Dialog()
            {
                _messageMap = new();
            }

            public void AddOrAcknowledge(ListenResponse response)
            {
                if (!_messageMap.TryGetValue(response.Message.MessageId, out Message message))
                {
                    message = new()
                    {
                        Data = response.Message,
                        SequenceNumber = response.SequenceNumber,
                    };
                    _messageMap.TryAdd(response.Message.MessageId, message);
                }

                message.AckStatus = AckStatuses.Map(response.Message.AckType);
            }

            public IEnumerable<Message> GetMessages()
            {
                foreach (KeyValuePair<long,Message> pair in _messageMap)
                {
                    yield return pair.Value;
                }
            }
        }

        private static class AckStatuses
        {
            public static readonly string Delivered = "Delivered";
            public static readonly string Processed = "Processed";
            public static readonly string Lost = "Lost";

            public static string Map(AckType ack)
            {
                switch (ack)
                {
                    case AckType.Delivered: return Delivered;
                    case AckType.Processed: return Processed;
                    case AckType.Lost: return Lost;
                    default: return string.Empty;
                }
            }
        }
    }
}
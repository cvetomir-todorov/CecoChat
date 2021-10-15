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

                message.AckStatus = AckStatuses.Map(response.Message.Status);
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
            private static readonly string Delivered = "Delivered";
            private static readonly string Processed = "Processed";
            private static readonly string Lost = "Lost";
            private static readonly string Unprocessed = "Unprocessed"; 

            public static string Map(ClientMessageStatus ack)
            {
                switch (ack)
                {
                    case ClientMessageStatus.Delivered: return Delivered;
                    case ClientMessageStatus.Processed: return Processed;
                    case ClientMessageStatus.Lost: return Lost;
                    case ClientMessageStatus.Unprocessed: return Unprocessed;
                    default: return string.Empty;
                }
            }
        }
    }
}
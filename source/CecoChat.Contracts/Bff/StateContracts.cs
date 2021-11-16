using System;
using System.Collections.Generic;

namespace CecoChat.Contracts.Bff
{
    public sealed class GetChatsRequest
    {
        public DateTime NewerThan { get; set; }
    }

    public sealed class GetChatsResponse
    {
        public List<ChatState> Chats { get; set; }
    }

    public sealed class ChatState
    {
        public string ChatID { get; set; }
        public long NewestMessage { get; set; }
        public long OtherUserDelivered { get; set; }
        public long OtherUserSeen { get; set; }
    }
}
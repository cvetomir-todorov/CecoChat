using System;

namespace CecoChat.Client.Wpf.Clients.Messaging
{
    /// <summary>
    /// Generates an ASCII symbol string representing a message ID. Length is 6:
    /// 2 bytes for user ID
    /// 2 bytes for randomly generated local session ID
    /// 2 bytes for UTC now ticks
    /// </summary>
    public sealed class MessageIDGenerator
    {
        private static readonly string _encode32chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";
        private static long _localSessionID;

        public MessageIDGenerator()
        {
            Random random = new();
            _localSessionID = random.Next() + ((long) random.Next() << 32);
        }

        public string GenerateMessageID(long userID)
        {
            long utcNow = DateTime.UtcNow.Ticks;

            string messageID = string.Create(length: 16, state: 0, (span, _) =>
            {
                // user ID
                span[0] = _encode32chars[(int) (userID >> 32)];
                span[1] = _encode32chars[(int) userID];

                // local session ID
                span[2] = _encode32chars[(int) (_localSessionID >> 32)];
                span[3] = _encode32chars[(int) _localSessionID];

                // UTC now
                span[4] = _encode32chars[(int) (utcNow >> 32)];
                span[5] = _encode32chars[(int) utcNow];
            });

            return messageID;
        }
    }
}

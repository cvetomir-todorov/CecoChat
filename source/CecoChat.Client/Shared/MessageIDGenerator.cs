using System;

namespace CecoChat.Client.Shared
{
    public sealed class MessageIDGenerator
    {
        private readonly string _encode32chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

        public string GenerateMessageID()
        {
            string correlationID = string.Create(length: 13, state: DateTime.UtcNow.Ticks, (span, id) =>
            {
                span[0] = _encode32chars[(int)(id >> 60) & 31];
                span[1] = _encode32chars[(int)(id >> 55) & 31];
                span[2] = _encode32chars[(int)(id >> 50) & 31];
                span[3] = _encode32chars[(int)(id >> 45) & 31];
                span[4] = _encode32chars[(int)(id >> 40) & 31];
                span[5] = _encode32chars[(int)(id >> 35) & 31];
                span[6] = _encode32chars[(int)(id >> 30) & 31];
                span[7] = _encode32chars[(int)(id >> 25) & 31];
                span[8] = _encode32chars[(int)(id >> 20) & 31];
                span[9] = _encode32chars[(int)(id >> 15) & 31];
                span[10] = _encode32chars[(int)(id >> 10) & 31];
                span[11] = _encode32chars[(int)(id >> 5) & 31];
                span[12] = _encode32chars[(int)id & 31];
            });

            return correlationID;
        }
    }
}

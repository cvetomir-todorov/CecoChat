using System;
using System.Threading;

namespace CecoChat.ConsoleClient
{
    public sealed class CorrelationIDGenerator
    {
        private readonly string _encode32chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";
        private long _lastID = DateTime.UtcNow.Ticks;

        public string GenerateCorrelationID()
        {
            long newID = Interlocked.Increment(ref _lastID);
            string correlationID = string.Create(length: 16, state: newID, (span, id) =>
            {
                // ReSharper disable ShiftExpressionRightOperandNotEqualRealCount
                span[0] = _encode32chars[(int) (id >> 60) & 31];
                span[1] = _encode32chars[(int) (id >> 56) & 31];
                span[2] = _encode32chars[(int) (id >> 52) & 31];
                span[3] = _encode32chars[(int) (id >> 48) & 31];
                span[4] = _encode32chars[(int) (id >> 44) & 31];
                span[5] = _encode32chars[(int) (id >> 40) & 31];
                span[6] = _encode32chars[(int) (id >> 36) & 31];
                span[7] = _encode32chars[(int) (id >> 32) & 31];
                span[8] = _encode32chars[(int) (id >> 28) & 31];
                span[9] = _encode32chars[(int) (id >> 24) & 31];
                span[10] = _encode32chars[(int) (id >> 20) & 31];
                span[11] = _encode32chars[(int) (id >> 16) & 31];
                span[12] = _encode32chars[(int) (id >> 12) & 31];
                span[13] = _encode32chars[(int) (id >> 8) & 31];
                span[14] = _encode32chars[(int) (id >> 4) & 31];
                span[15] = _encode32chars[(int) id & 31];
                // ReSharper restore ShiftExpressionRightOperandNotEqualRealCount
            });

            return correlationID;
        }
    }
}

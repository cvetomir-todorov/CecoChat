using System;

namespace CecoChat.Data
{
    public static class DataUtility
    {
        public static string CreateChatID(long userID1, long userID2)
        {
            long min = Math.Min(userID1, userID2);
            long max = Math.Max(userID1, userID2);

            return $"{min}-{max}";
        }
    }
}
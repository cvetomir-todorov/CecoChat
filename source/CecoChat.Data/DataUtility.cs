namespace CecoChat.Data;

public static class DataUtility
{
    private const char Separator = '-';

    public static string CreateChatId(long userId1, long userId2)
    {
        if (userId1 == userId2)
        {
            throw new ArgumentException("User IDs should be different.", paramName: nameof(userId1));
        }

        long min = Math.Min(userId1, userId2);
        long max = Math.Max(userId1, userId2);

        return $"{min}{Separator}{max}";
    }

    public static long GetOtherUserId(string chatId, long currentUserId)
    {
        if (string.IsNullOrWhiteSpace(chatId) || !chatId.Contains(Separator))
        {
            throw new ArgumentException($"Argument {nameof(chatId)} should not be null/whitespace and should contain a '{Separator}' separator.");
        }

        string[] userIds = chatId.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        if (userIds.Length != 2)
        {
            throw new ArgumentException($"Argument {nameof(chatId)} isn't in the format '123{Separator}456' where 123 and 456 are used IDs.");
        }
        if (!long.TryParse(userIds[0], out long leftUserId))
        {
            throw new ArgumentException($"Argument {nameof(chatId)} doesn't have a valid user ID left from the separator.");
        }
        if (!long.TryParse(userIds[1], out long rightUserId))
        {
            throw new ArgumentException($"Argument {nameof(chatId)} doesn't have a valid user ID right from the separator.");
        }

        long otherUserId = GetOtherUserId(currentUserId, leftUserId, rightUserId);
        return otherUserId;
    }

    public static long GetOtherUserId(long currentUserId, long userId1, long userId2)
    {
        if (userId1 != currentUserId && userId2 != currentUserId)
        {
            throw new ArgumentException($"Either {nameof(userId1)} = {userId1} or {nameof(userId2)} = {userId2} should be equal to {nameof(currentUserId)} = {currentUserId}.");
        }

        long otherUserId = userId1 != currentUserId ? userId1 : userId2;
        return otherUserId;
    }
}

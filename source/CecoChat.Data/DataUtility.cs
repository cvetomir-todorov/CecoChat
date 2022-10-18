namespace CecoChat.Data;

public static class DataUtility
{
    private const char Separator = '-';

    public static string CreateChatID(long userID1, long userID2)
    {
        long min = Math.Min(userID1, userID2);
        long max = Math.Max(userID1, userID2);

        return $"{min}{Separator}{max}";
    }

    public static long GetOtherUsedID(string chatID, long currentUserID)
    {
        if (string.IsNullOrWhiteSpace(chatID) || !chatID.Contains(Separator))
            throw new ArgumentException($"Argument {nameof(chatID)} should not be null/whitespace and should contain a '{Separator}' separator.");

        string[] userIDs = chatID.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        if (userIDs.Length != 2)
            throw new ArgumentException($"Argument {nameof(chatID)} isn't in the format '123{Separator}456' where 123 and 456 are used IDs.");
        if (!long.TryParse(userIDs[0], out long leftUserID))
            throw new ArgumentException($"Argument {nameof(chatID)} doesn't have a valid user ID left from the separator.");
        if (!long.TryParse(userIDs[1], out long rightUserID))
            throw new ArgumentException($"Argument {nameof(chatID)} doesn't have a valid user ID right from the separator.");
        if (leftUserID != currentUserID && rightUserID != currentUserID)
            throw new ArgumentException($"Argument {nameof(chatID)} should contain the argument {nameof(currentUserID)}.");

        long otherUserID = leftUserID != currentUserID ? leftUserID : rightUserID;
        return otherUserID;
    }
}
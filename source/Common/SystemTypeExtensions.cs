namespace Common;

public static class SystemTypeExtensions
{
    public static int ToMillisInt32(this TimeSpan interval)
    {
        return (int)interval.TotalMilliseconds;
    }
}

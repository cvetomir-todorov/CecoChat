namespace Common;

public static class Snowflake
{
    public static readonly DateTime Epoch = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}

public static class SnowflakeExtensions
{
    private static readonly long EpochTicks = Snowflake.Epoch.Ticks;

    public static DateTime ToTimestamp(this long snowflake)
    {
        // first 22 bits are for generator and sequence, the rest 41 are for time
        // the sign bit is unused
        // the 41 bits for timestamp are incremented each tick
        // each tick is 1 ms so we have to convert it to system time ticks
        long snowflakeTicks = (snowflake >> 22) * TimeSpan.TicksPerMillisecond;
        long timestampTicks = EpochTicks + snowflakeTicks;
        DateTime timestamp = new(timestampTicks, DateTimeKind.Utc);
        return timestamp;
    }

    public static long ToSnowflake(this DateTime timestamp)
    {
        return ToSnowflakeInternal(timestamp, Rounding.Round);
    }

    public static long ToSnowflakeCeiling(this DateTime timestamp)
    {
        return ToSnowflakeInternal(timestamp, Rounding.Ceiling);
    }

    public static long ToSnowflakeFloor(this DateTime timestamp)
    {
        return ToSnowflakeInternal(timestamp, Rounding.Floor);
    }

    private enum Rounding
    {
        Round, Ceiling, Floor
    }

    private static readonly long HalfTicksPerMillisecond = TimeSpan.TicksPerMillisecond / 2;

    private static long ToSnowflakeInternal(DateTime timestamp, Rounding rounding)
    {
        long timestampTicks = timestamp.Ticks;
        long snowflakeTicks = timestampTicks - EpochTicks;
        long snowflakeMs = snowflakeTicks / TimeSpan.TicksPerMillisecond;

        switch (rounding)
        {
            case Rounding.Round:
                if (snowflakeTicks % TimeSpan.TicksPerMillisecond >= HalfTicksPerMillisecond)
                {
                    snowflakeMs++;
                }
                break;
            case Rounding.Ceiling:
                if (snowflakeTicks % TimeSpan.TicksPerMillisecond > 0)
                {
                    snowflakeMs++;
                }
                break;
            case Rounding.Floor:
                break;
            default:
                throw new InvalidOperationException($"{typeof(Rounding).FullName} value {rounding} is not supported.");
        }

        long snowflake = snowflakeMs << 22;
        return snowflake;
    }
}

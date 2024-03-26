using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Common;

public interface IClock
{
    DateTime GetNowUtc();

    DateTime GetNowLocal();
}

public sealed class MonotonicClock : IClock
{
    private static readonly DateTime StartUtc;
    private static readonly Stopwatch Stopwatch;

    static MonotonicClock()
    {
        Stopwatch = new Stopwatch();
        Stopwatch.Start();
        StartUtc = DateTime.UtcNow;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime GetNowUtc()
    {
        return StartUtc.Add(Stopwatch.Elapsed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime GetNowLocal()
    {
        return GetNowUtc().ToLocalTime();
    }
}

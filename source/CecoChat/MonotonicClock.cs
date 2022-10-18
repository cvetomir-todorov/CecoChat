using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CecoChat;

public interface IClock
{
    DateTime GetNowUtc();

    DateTime GetNowLocal();
}

public sealed class MonotonicClock : IClock
{
    private static readonly DateTime _startUtc;
    private static readonly Stopwatch _stopwatch;

    static MonotonicClock()
    {
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
        _startUtc = DateTime.UtcNow;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime GetNowUtc()
    {
        return _startUtc.Add(_stopwatch.Elapsed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime GetNowLocal()
    {
        return GetNowUtc().ToLocalTime();
    }
}
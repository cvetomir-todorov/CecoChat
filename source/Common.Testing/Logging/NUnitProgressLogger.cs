using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Common.Testing.Logging;

public class NUnitProgressLogger<TCategoryName> : ILogger<TCategoryName>
{
#pragma warning disable IDE0060
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
#pragma warning restore IDE0060

#pragma warning disable IDE0060
    public bool IsEnabled(LogLevel logLevel) => true;
#pragma warning restore IDE0060

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string message = formatter.Invoke(state, exception);
        string level;
        switch (logLevel)
        {
            case LogLevel.Trace:
                level = "VRB";
                break;
            case LogLevel.Debug:
                level = "DBG";
                break;
            case LogLevel.Information:
                level = "INF";
                break;
            case LogLevel.Warning:
                level = "WRN";
                break;
            case LogLevel.Error:
                level = "ERR";
                break;
            case LogLevel.Critical:
                level = "CRI";
                break;
            default:
                throw new EnumValueNotSupportedException(logLevel);
        }

        TestContext.Progress.WriteLine("[{0:HH:mm:ss.fff} {1}] {2} {3}", DateTime.UtcNow, level, message, exception);
        TestContext.Progress.Flush();
    }
}

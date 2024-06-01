using NUnit.Framework;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Common.Testing.Logging;

public sealed class NUnitSerilogSink : ILogEventSink, IDisposable
{
    private readonly StringWriter _textWriter;
    private readonly ITextFormatter _textFormatter;

    public NUnitSerilogSink(ITextFormatter textFormatter)
    {
        _textWriter = new StringWriter();
        _textFormatter = textFormatter;
    }

    public void Dispose()
    {
        _textWriter.Dispose();
    }

    public void Emit(LogEvent logEvent)
    {
        _textFormatter.Format(logEvent, _textWriter);
        TestContext.Progress.Write(_textWriter.ToString());
        _textWriter.GetStringBuilder().Clear();
    }
}

public static class NUnitSinkRegistrations
{
    public static LoggerConfiguration NUnit(
        this LoggerSinkConfiguration sinkConfiguration,
        string outputTemplate,
        IFormatProvider? formatProvider = null,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
        LoggingLevelSwitch? levelSwitch = null)
    {
        ITextFormatter textFormatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
        NUnitSerilogSink nUnitSerilogSink = new(textFormatter);

        return sinkConfiguration.Sink(nUnitSerilogSink, restrictedToMinimumLevel, levelSwitch);
    }
}

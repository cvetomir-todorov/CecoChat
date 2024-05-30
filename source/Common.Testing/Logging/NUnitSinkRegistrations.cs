using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Common.Testing.Logging;

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
        NUnitSink nUnitSink = new(textFormatter);

        return sinkConfiguration.Sink(nUnitSink, restrictedToMinimumLevel, levelSwitch);
    }
}

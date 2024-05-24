using NUnit.Framework;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace CecoChat.IdGen.Testing.Logging;

public sealed class NUnitSink : ILogEventSink, IDisposable
{
    private readonly StringWriter _textWriter;
    private readonly ITextFormatter _textFormatter;

    public NUnitSink(ITextFormatter textFormatter)
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

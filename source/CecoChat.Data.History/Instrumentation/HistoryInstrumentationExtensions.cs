using OpenTelemetry.Trace;

namespace CecoChat.Data.History.Instrumentation
{
    public static class HistoryInstrumentationExtensions
    {
        public static TracerProviderBuilder AddHistoryInstrumentation(this TracerProviderBuilder builder)
        {
            return builder.AddSource(HistoryInstrumentation.ActivitySource.Name);
        }
    }
}
using OpenTelemetry.Trace;

namespace CecoChat.Data.History.Instrumentation
{
    public static class HistoryInstrumentationRegistrations
    {
        public static TracerProviderBuilder AddHistoryInstrumentation(this TracerProviderBuilder builder)
        {
            return builder.AddSource(HistoryInstrumentation.ActivitySource.Name);
        }
    }
}
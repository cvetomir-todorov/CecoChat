using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cassandra;
using CecoChat.Otel;
using Microsoft.Extensions.Logging;

namespace CecoChat.Cassandra.Telemetry;

public interface ICassandraTelemetry : IDisposable
{
    void EnableMetrics(ActivitySource activitySource, string queryDurationHistogramName);

    RowSet ExecuteStatement(ISession session, IStatement statement, ActivitySource activitySource, string operationName, Action<Activity>? enrich = null);

    Task<RowSet> ExecuteStatementAsync(ISession session, IStatement statement, ActivitySource activitySource, string operationName, Action<Activity>? enrich = null);
}

public sealed class CassandraTelemetry : ICassandraTelemetry
{
    private readonly ILogger _logger;
    private readonly ITelemetry _telemetry;
    private Meter? _meter;
    private Histogram<double>? _queryDuration;

    public CassandraTelemetry(ILogger<CassandraTelemetry> logger, ITelemetry telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }

    public void EnableMetrics(ActivitySource activitySource, string queryDurationHistogramName)
    {
        _meter = new Meter(activitySource.Name);
        _queryDuration = _meter.CreateHistogram<double>(queryDurationHistogramName, "ms", "measures the duration of the outbound Cassandra query");
    }

    public RowSet ExecuteStatement(ISession session, IStatement statement, ActivitySource activitySource, string operationName, Action<Activity>? enrich = null)
    {
        Activity activity = StartClientActivity(activitySource, operationName, enrich);

        try
        {
            RowSet rows = session.Execute(statement);
            activity.SetStatus(ActivityStatusCode.Ok);
            return rows;
        }
        catch
        {
            activity.SetStatus(ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            activity.Stop();
            RecordMetrics(activity);
        }
    }

    public async Task<RowSet> ExecuteStatementAsync(ISession session, IStatement statement, ActivitySource activitySource, string operationName, Action<Activity>? enrich = null)
    {
        Activity activity = StartClientActivity(activitySource, operationName, enrich);

        try
        {
            RowSet rowSet = await session.ExecuteAsync(statement);
            activity.SetStatus(ActivityStatusCode.Ok);
            return rowSet;
        }
        catch
        {
            activity.SetStatus(ActivityStatusCode.Error);
            throw;
        }
        finally
        {
            activity.Stop();
            RecordMetrics(activity);
        }
    }

    private Activity StartClientActivity(ActivitySource activitySource, string operationName, Action<Activity>? enrich)
    {
        Activity activity = _telemetry.Start(operationName, activitySource, ActivityKind.Client, Activity.Current?.Context);

        if (activity.IsAllDataRequested && enrich != null)
        {
            enrich(activity);
        }

        return activity;
    }

    private void RecordMetrics(Activity activity)
    {
        if (!activity.IsStopped)
        {
            _logger.LogWarning("Logging metrics for activity that is not stopped");
            return;
        }
        if (_queryDuration != null)
        {
            TagList tags = new();
            foreach (KeyValuePair<string, string?> tag in activity.Tags)
            {
                tags.Add(tag.Key, tag.Value);
            }

            _queryDuration.Record(activity.Duration.TotalMilliseconds, tags);
        }
    }
}

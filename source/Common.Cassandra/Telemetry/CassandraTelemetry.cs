using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cassandra;
using Common.OpenTelemetry;
using OpenTelemetry.Trace;

namespace Common.Cassandra.Telemetry;

public interface ICassandraTelemetry : IDisposable
{
    void EnableMetrics(ActivitySource activitySource, string queryDurationHistogramName);

    RowSet ExecuteStatement(ISession session, IStatement statement, ActivitySource activitySource, string activityName, string operationName, Action<Activity>? enrich = null);

    Task<RowSet> ExecuteStatementAsync(ISession session, IStatement statement, ActivitySource activitySource, string activityName, string operationName, Action<Activity>? enrich = null);
}

public sealed class CassandraTelemetry : ICassandraTelemetry
{
    private Meter? _meter;
    private Histogram<double>? _queryDuration;

    public void Dispose()
    {
        _meter?.Dispose();
    }

    public void EnableMetrics(ActivitySource activitySource, string queryDurationHistogramName)
    {
        _meter = new Meter(activitySource.Name);
        _queryDuration = _meter.CreateHistogram<double>(queryDurationHistogramName, "ms", "measures the duration of the outbound Cassandra query");
    }

    public RowSet ExecuteStatement(ISession session, IStatement statement, ActivitySource activitySource, string activityName, string operationName, Action<Activity>? enrich = null)
    {
        Activity? activity = StartActivity(session, activitySource, activityName, operationName, enrich);

        try
        {
            RowSet rows = session.Execute(statement);
            Succeed(activity);

            return rows;
        }
        catch (Exception exception)
        {
            Fail(activity, exception);
            throw;
        }
    }

    public async Task<RowSet> ExecuteStatementAsync(ISession session, IStatement statement, ActivitySource activitySource, string activityName, string operationName, Action<Activity>? enrich = null)
    {
        Activity? activity = StartActivity(session, activitySource, activityName, operationName, enrich);

        try
        {
            RowSet rowSet = await session.ExecuteAsync(statement);
            Succeed(activity);

            return rowSet;
        }
        catch (Exception exception)
        {
            Fail(activity, exception);
            throw;
        }
    }

    private static Activity? StartActivity(ISession session, ActivitySource activitySource, string activityName, string operationName, Action<Activity>? enrich)
    {
        Activity? activity = activitySource.StartActivity(activityName, ActivityKind.Client);

        if (activity != null)
        {
            activity.DisplayName = $"{session.Keyspace}/{operationName}";

            activity.SetTag(OtelInstrumentation.Keys.DbSystem, CassandraInstrumentation.Values.DbSystemCassandra);
            activity.SetTag(OtelInstrumentation.Keys.DbName, session.Keyspace);
            activity.SetTag(OtelInstrumentation.Keys.DbSessionName, session.SessionName);
            activity.SetTag(OtelInstrumentation.Keys.DbOperation, operationName);

            if (activity.IsAllDataRequested)
            {
                enrich?.Invoke(activity);
            }
        }

        return activity;
    }

    private void Succeed(Activity? activity)
    {
        if (activity == null)
        {
            return;
        }

        activity.Stop();
        activity.SetStatus(ActivityStatusCode.Ok);
        Record(activity);
    }

    private void Fail(Activity? activity, Exception? exception)
    {
        if (activity == null)
        {
            return;
        }

        activity.Stop();

        if (activity.IsAllDataRequested && exception != null)
        {
            activity.SetStatus(Status.Error.WithDescription(exception.Message));
        }
        else
        {
            activity.SetStatus(ActivityStatusCode.Error);
        }

        Record(activity);
    }

    private void Record(Activity activity)
    {
        if (!activity.IsStopped)
        {
            throw new InvalidOperationException("Activity should have already been stopped");
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

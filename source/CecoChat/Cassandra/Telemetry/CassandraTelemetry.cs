using System.Diagnostics;
using Cassandra;
using CecoChat.Otel;

namespace CecoChat.Cassandra.Telemetry;

public interface ICassandraTelemetry
{
    RowSet ExecuteStatement(ISession session, IStatement statement, ActivitySource activitySource, string operationName, Action<Activity>? enrich = null);

    Task<RowSet> ExecuteStatementAsync(ISession session, IStatement statement, ActivitySource activitySource, string operationName, Action<Activity>? enrich = null);
}

public class CassandraTelemetry : ICassandraTelemetry
{
    private readonly ITelemetry _telemetry;

    public CassandraTelemetry(ITelemetry telemetry)
    {
        _telemetry = telemetry;
    }

    public RowSet ExecuteStatement(ISession session, IStatement statement, ActivitySource activitySource, string operationName, Action<Activity>? enrich = null)
    {
        Activity activity = _telemetry.Start(operationName, activitySource, ActivityKind.Client, Activity.Current?.Context);

        if (activity.IsAllDataRequested && enrich != null)
        {
            enrich(activity);
        }

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
        }
    }

    public async Task<RowSet> ExecuteStatementAsync(ISession session, IStatement statement, ActivitySource activitySource, string operationName, Action<Activity>? enrich = null)
    {
        Activity activity = _telemetry.Start(operationName, activitySource, ActivityKind.Client, Activity.Current?.Context);

        if (activity.IsAllDataRequested && enrich != null)
        {
            enrich(activity);
        }

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
        }
    }
}

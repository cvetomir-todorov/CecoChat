using System.Diagnostics;
using System.Linq.Expressions;
using OpenTelemetry.Trace;

namespace CecoChat.Tracing;

public interface IActivityUtility
{
    Activity Start(string operationName, ActivitySource source, ActivityKind kind, ActivityContext? parentContext);

    /// <summary>
    /// By default calling <see cref="Activity.Stop"/> sets the <see cref="Activity.Current"/>
    /// to the <see cref="Activity.Parent"/> of the stopped activity which may not be desired.
    /// </summary>
    void Stop(Activity? activity, bool success, bool relyOnDefaultPolicyOfSettingCurrentActivity = true);
}

public sealed class ActivityUtility : IActivityUtility
{
    public Activity Start(string operationName, ActivitySource source, ActivityKind kind, ActivityContext? parentContext)
    {
        // always create a new activity because ActivitySource.Start may return null if the trace ID is not sampled
        // otherwise child activities are eligible for sampling because without a parent they have a new trace ID
        Activity activity = new(operationName);

        if (parentContext.HasValue)
        {
            activity.SetParentId(parentContext.Value.TraceId, parentContext.Value.SpanId, parentContext.Value.TraceFlags);
            activity.TraceStateString = parentContext.Value.TraceState;
        }

        SetKindProperty(activity, ActivityKind.Consumer);
        SetSourceProperty(activity, source);

        activity.Start();

        return activity;
    }

    private static Action<Activity, ActivityKind> SetKindProperty => CreateActivityKindSetter();

    private static Action<Activity, ActivitySource> SetSourceProperty => CreateActivitySourceSetter();

    private static Action<Activity, ActivitySource> CreateActivitySourceSetter()
    {
        ParameterExpression instance = Expression.Parameter(typeof(Activity), "instance");
        ParameterExpression propertyValue = Expression.Parameter(typeof(ActivitySource), "propertyValue");
        Expression body = Expression.Assign(Expression.Property(instance, nameof(Activity.Source)), propertyValue);
        return Expression.Lambda<Action<Activity, ActivitySource>>(body, instance, propertyValue).Compile();
    }

    private static Action<Activity, ActivityKind> CreateActivityKindSetter()
    {
        ParameterExpression instance = Expression.Parameter(typeof(Activity), "instance");
        ParameterExpression propertyValue = Expression.Parameter(typeof(ActivityKind), "propertyValue");
        Expression body = Expression.Assign(Expression.Property(instance, nameof(Activity.Kind)), propertyValue);
        return Expression.Lambda<Action<Activity, ActivityKind>>(body, instance, propertyValue).Compile();
    }

    public void Stop(Activity? activity, bool success, bool relyOnDefaultPolicyOfSettingCurrentActivity = true)
    {
        if (activity != null)
        {
            Status status = success ? Status.Ok : Status.Error;
            activity.SetStatus(status);

            Activity? currentActivity = Activity.Current;
            activity.Stop();

            if (!relyOnDefaultPolicyOfSettingCurrentActivity)
            {
                Activity.Current = currentActivity;
            }
        }
    }
}
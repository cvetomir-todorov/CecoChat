using System.Diagnostics;
using System.Text;
using Common.OpenTelemetry;
using Confluent.Kafka;
using OpenTelemetry.Trace;

namespace Common.Kafka.Telemetry;

public interface IKafkaTelemetry
{
    Activity? StartProducer<TKey, TValue>(Message<TKey, TValue> message, string producerId, string topic, int? partition = null);

    void StopProducer(Activity? activity, bool success, Exception? exception);

    Activity? StartConsumer<TKey, TValue>(ConsumeResult<TKey, TValue> consumeResult, string consumerId);

    void StopConsumer(Activity? activity, bool success, Exception? exception);
}

internal sealed class KafkaTelemetry : IKafkaTelemetry
{
    public Activity? StartProducer<TKey, TValue>(Message<TKey, TValue> message, string producerId, string topic, int? partition = null)
    {
        Activity? parent = Activity.Current;
        Activity? activity = KafkaInstrumentation.ActivitySource.StartActivity(KafkaInstrumentation.ActivityName, ActivityKind.Producer);

        // activity will be completed in the delivery handler thread
        // and we don't want to pollute the execution context
        // so we set the current activity to the previous one
        Activity.Current = parent;

        if (activity != null)
        {
            string displayName = $"{producerId} > Topic:{topic}";
            EnrichActivity(topic, partition, displayName, activity);
            InjectTraceData(activity.Context, message);
        }

        return activity;
    }

    public void StopProducer(Activity? activity, bool success, Exception? exception)
    {
        StopActivity(activity, success, exception);
    }

    public Activity? StartConsumer<TKey, TValue>(ConsumeResult<TKey, TValue> consumeResult, string consumerId)
    {
        if (!TryExtractTraceData(consumeResult, out ActivityContext parentContext))
        {
            // the trace isn't sampled, so we don't need to start a new activity
            return null;
        }

        Activity? activity = KafkaInstrumentation.ActivitySource.StartActivity(KafkaInstrumentation.ActivityName, ActivityKind.Consumer, parentContext);
        if (activity != null)
        {
            string displayName = $"{consumerId} < Topic:{consumeResult.Topic}";
            EnrichActivity(consumeResult.Topic, consumeResult.Partition, displayName, activity);
        }

        return activity;
    }

    public void StopConsumer(Activity? activity, bool success, Exception? exception)
    {
        StopActivity(activity, success, exception);
    }

    private static void InjectTraceData<TKey, TValue>(ActivityContext activityContext, Message<TKey, TValue> message)
    {
        byte[] traceIdBytes = new byte[16];
        activityContext.TraceId.CopyTo(traceIdBytes);
        byte[] spanIdBytes = new byte[8];
        activityContext.SpanId.CopyTo(spanIdBytes);
        byte[] traceFlagsBytes = BitConverter.GetBytes((int)activityContext.TraceFlags);

        message.Headers ??= new Headers();
        message.Headers.Add(OtelInstrumentation.Keys.HeaderTraceId, traceIdBytes);
        message.Headers.Add(OtelInstrumentation.Keys.HeaderSpanId, spanIdBytes);
        message.Headers.Add(OtelInstrumentation.Keys.HeaderTraceFlags, traceFlagsBytes);

        if (activityContext.TraceState != null)
        {
            byte[] traceStateBytes = Encoding.UTF8.GetBytes(activityContext.TraceState);
            message.Headers.Add(OtelInstrumentation.Keys.HeaderTraceState, traceStateBytes);
        }
    }

    private static bool TryExtractTraceData<TKey, TValue>(ConsumeResult<TKey, TValue> consumeResult, out ActivityContext activityContext)
    {
        if (consumeResult.Message.Headers == null)
        {
            activityContext = new();
            return false;
        }

        if (consumeResult.Message.Headers.TryGetLastBytes(OtelInstrumentation.Keys.HeaderTraceId, out byte[] traceIdBytes) &&
            consumeResult.Message.Headers.TryGetLastBytes(OtelInstrumentation.Keys.HeaderSpanId, out byte[] spanIdBytes) &&
            consumeResult.Message.Headers.TryGetLastBytes(OtelInstrumentation.Keys.HeaderTraceFlags, out byte[] traceFlagsBytes))
        {
            ActivityTraceId traceId = ActivityTraceId.CreateFromBytes(traceIdBytes);
            ActivitySpanId spanId = ActivitySpanId.CreateFromBytes(spanIdBytes);
            ActivityTraceFlags traceFlags = (ActivityTraceFlags)BitConverter.ToInt32(traceFlagsBytes);

            string? traceState = null;
            if (consumeResult.Message.Headers.TryGetLastBytes(OtelInstrumentation.Keys.HeaderTraceState, out byte[] traceStateBytes))
            {
                traceState = Encoding.UTF8.GetString(traceStateBytes);
            }

            activityContext = new ActivityContext(traceId, spanId, traceFlags, traceState, isRemote: true);
            return true;
        }
        else
        {
            activityContext = new();
            return false;
        }
    }

    private static void EnrichActivity(string topic, int? partition, string displayName, Activity activity)
    {
        activity.DisplayName = displayName;

        activity.SetTag(OtelInstrumentation.Keys.MessagingSystem, KafkaInstrumentation.Values.MessagingSystemKafka);
        activity.SetTag(OtelInstrumentation.Keys.MessagingDestinationKind, KafkaInstrumentation.Values.MessagingDestinationKindTopic);
        activity.SetTag(OtelInstrumentation.Keys.MessagingDestination, topic);

        if (partition.HasValue)
        {
            activity.SetTag(KafkaInstrumentation.Keys.MessagingKafkaPartition, partition.Value);
        }
    }

    private static void StopActivity(Activity? activity, bool success, Exception? exception)
    {
        if (activity != null)
        {
            Status status;
            if (success)
            {
                status = Status.Ok;
            }
            else
            {
                if (exception != null)
                {
                    status = Status.Error.WithDescription(exception.Message);
                }
                else
                {
                    status = Status.Error;
                }
            }

            activity.SetStatus(status);
            activity.Stop();
        }
    }
}

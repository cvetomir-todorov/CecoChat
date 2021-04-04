using System;
using System.Diagnostics;
using System.Text;
using Confluent.Kafka;

namespace CecoChat.Kafka.Instrumentation
{
    public interface IKafkaActivityUtility
    {
        void EnrichActivity(string topic, int? partition, string displayName, Activity activity);

        void InjectTraceData<TKey, TValue>(Activity activity, Message<TKey, TValue> message);

        void ExtractTraceData<TKey, TValue>(Message<TKey, TValue> message, Activity activity);
    }

    internal sealed class KafkaActivityUtility : IKafkaActivityUtility
    {
        public void EnrichActivity(string topic, int? partition, string displayName, Activity activity)
        {
            if (activity.IsAllDataRequested)
            {
                activity.DisplayName = displayName;

                activity.SetTag(KafkaInstrumentation.Keys.TagMessagingSystem, KafkaInstrumentation.Values.TagMessagingSystemKafka);
                activity.SetTag(KafkaInstrumentation.Keys.TagMessagingDestinationKind, KafkaInstrumentation.Values.TagMessagingDestinationKindTopic);
                activity.SetTag(KafkaInstrumentation.Keys.TagMessagingDestination, topic);

                if (partition.HasValue)
                {
                    activity.SetTag(KafkaInstrumentation.Keys.TagMessagingKafkaPartition, partition.Value);
                }
            }
        }

        public void InjectTraceData<TKey, TValue>(Activity activity, Message<TKey, TValue> message)
        {
            byte[] traceIdBytes = new byte[16];
            activity.TraceId.CopyTo(traceIdBytes);
            byte[] spanIdBytes = new byte[8];
            activity.SpanId.CopyTo(spanIdBytes);
            byte[] traceFlagsBytes = BitConverter.GetBytes((int)activity.ActivityTraceFlags);

            message.Headers ??= new Headers();
            message.Headers.Add(KafkaInstrumentation.Keys.HeaderTraceId, traceIdBytes);
            message.Headers.Add(KafkaInstrumentation.Keys.HeaderSpanId, spanIdBytes);
            message.Headers.Add(KafkaInstrumentation.Keys.HeaderTraceFlags, traceFlagsBytes);

            if (activity.TraceStateString != null)
            {
                byte[] traceStateBytes = Encoding.UTF8.GetBytes(activity.TraceStateString);
                message.Headers.Add(KafkaInstrumentation.Keys.HeaderTraceState, traceStateBytes);
            }
        }

        public void ExtractTraceData<TKey, TValue>(Message<TKey, TValue> message, Activity activity)
        {
            if (message.Headers == null)
            {
                return;
            }

            if (message.Headers.TryGetLastBytes(KafkaInstrumentation.Keys.HeaderTraceId, out byte[] traceIdBytes) &&
                message.Headers.TryGetLastBytes(KafkaInstrumentation.Keys.HeaderSpanId, out byte[] spanIdBytes) &&
                message.Headers.TryGetLastBytes(KafkaInstrumentation.Keys.HeaderTraceFlags, out byte[] traceFlagsBytes))
            {
                ActivityTraceId traceId = ActivityTraceId.CreateFromBytes(traceIdBytes);
                ActivitySpanId spanId = ActivitySpanId.CreateFromBytes(spanIdBytes);
                ActivityTraceFlags traceFlags = (ActivityTraceFlags)BitConverter.ToInt32(traceFlagsBytes);

                activity.SetParentId(traceId, spanId, traceFlags);
            }

            if (message.Headers.TryGetLastBytes(KafkaInstrumentation.Keys.HeaderTraceState, out byte[] traceStateBytes))
            {
                string traceState = Encoding.UTF8.GetString(traceStateBytes);
                activity.TraceStateString = traceState;
            }
        }
    }
}

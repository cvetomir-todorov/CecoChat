﻿using System;
using System.Diagnostics;
using System.Text;
using CecoChat.Tracing;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace CecoChat.Kafka.Instrumentation
{
    public interface IKafkaActivityUtility
    {
        Activity StartProducer<TKey, TValue>(Message<TKey, TValue> message, string producerID, string topic, int? partition = null);

        void StopProducer(Activity activity, bool operationSuccess);

        Activity StartConsumer<TKey, TValue>(ConsumeResult<TKey, TValue> consumeResult, string consumerID);

        void StopConsumer(Activity activity, bool operationSuccess);
    }

    internal sealed class KafkaActivityUtility : IKafkaActivityUtility
    {
        private readonly ILogger _logger;
        private readonly IActivityUtility _activityUtility;

        public KafkaActivityUtility(
            ILogger<KafkaActivityUtility> logger,
            IActivityUtility activityUtility)
        {
            _logger = logger;
            _activityUtility = activityUtility;
        }

        public Activity StartProducer<TKey, TValue>(Message<TKey, TValue> message, string producerID, string topic, int? partition = null)
        {
            Activity parent = Activity.Current;
            Activity activity = _activityUtility.Start(
                KafkaInstrumentation.Operations.Production,
                KafkaInstrumentation.ActivitySource,
                ActivityKind.Producer,
                parent?.Context);

            // activity will be completed in the delivery handler thread
            // and we don't want to pollute the execution context
            // so we set the current activity to the previous one
            Activity.Current = parent;

            string displayName = $"{activity.OperationName}/Producer:{producerID} -> Topic:{topic}";
            EnrichActivity(topic, partition, displayName, activity);
            InjectTraceData(activity.Context, message);

            return activity;
        }

        public void StopProducer(Activity activity, bool operationSuccess)
        {
            // do not change the Activity.Current
            _activityUtility.Stop(activity, operationSuccess, relyOnDefaultPolicyOfSettingCurrentActivity: false);
        }

        public Activity StartConsumer<TKey, TValue>(ConsumeResult<TKey, TValue> consumeResult, string consumerID)
        {
            if (!TryExtractTraceData(consumeResult, out ActivityContext parentContext))
            {
                _logger.LogWarning("Message from topic {0} in partition {1} has missing trace ID data.",
                    consumeResult.Topic, consumeResult.Partition.Value);
                return null;
            }

            Activity activity = _activityUtility.Start(
                KafkaInstrumentation.Operations.Consumption,
                KafkaInstrumentation.ActivitySource,
                ActivityKind.Consumer,
                parentContext);

            string displayName = $"{activity.OperationName}/Topic:{consumeResult.Topic} -> Consumer:{consumerID}";
            EnrichActivity(consumeResult.Topic, consumeResult.Partition, displayName, activity);

            return activity;
        }

        public void StopConsumer(Activity activity, bool operationSuccess)
        {
            // do not change the Activity.Current
            _activityUtility.Stop(activity, operationSuccess, relyOnDefaultPolicyOfSettingCurrentActivity: false);
        }

        private static void InjectTraceData<TKey, TValue>(ActivityContext activityContext, Message<TKey, TValue> message)
        {
            byte[] traceIdBytes = new byte[16];
            activityContext.TraceId.CopyTo(traceIdBytes);
            byte[] spanIdBytes = new byte[8];
            activityContext.SpanId.CopyTo(spanIdBytes);
            byte[] traceFlagsBytes = BitConverter.GetBytes((int)activityContext.TraceFlags);

            message.Headers ??= new Headers();
            message.Headers.Add(KafkaInstrumentation.Keys.HeaderTraceId, traceIdBytes);
            message.Headers.Add(KafkaInstrumentation.Keys.HeaderSpanId, spanIdBytes);
            message.Headers.Add(KafkaInstrumentation.Keys.HeaderTraceFlags, traceFlagsBytes);

            if (activityContext.TraceState != null)
            {
                byte[] traceStateBytes = Encoding.UTF8.GetBytes(activityContext.TraceState);
                message.Headers.Add(KafkaInstrumentation.Keys.HeaderTraceState, traceStateBytes);
            }
        }

        private static bool TryExtractTraceData<TKey, TValue>(ConsumeResult<TKey, TValue> consumeResult, out ActivityContext activityContext)
        {
            if (consumeResult.Message.Headers == null)
            {
                activityContext = new();
                return false;
            }

            if (consumeResult.Message.Headers.TryGetLastBytes(KafkaInstrumentation.Keys.HeaderTraceId, out byte[] traceIdBytes) &&
                consumeResult.Message.Headers.TryGetLastBytes(KafkaInstrumentation.Keys.HeaderSpanId, out byte[] spanIdBytes) &&
                consumeResult.Message.Headers.TryGetLastBytes(KafkaInstrumentation.Keys.HeaderTraceFlags, out byte[] traceFlagsBytes))
            {
                ActivityTraceId traceId = ActivityTraceId.CreateFromBytes(traceIdBytes);
                ActivitySpanId spanId = ActivitySpanId.CreateFromBytes(spanIdBytes);
                ActivityTraceFlags traceFlags = (ActivityTraceFlags)BitConverter.ToInt32(traceFlagsBytes);

                string traceState = null;
                if (consumeResult.Message.Headers.TryGetLastBytes(KafkaInstrumentation.Keys.HeaderTraceState, out byte[] traceStateBytes))
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
    }
}

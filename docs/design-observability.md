# Distributed tracing

OpenTelemetry is utilized to make flow of communication and data exchange observable and help resolve production issues.

## Resources

Each server is represented as a separate resource with namespace, name and version.

## Instrumentation

The following instrumentations are used:
* ASP.NET Core (with gRPC support for incoming requests) which is out-of-the box
* gRPC custom instrumentation for server streaming which wasn't present/working
* Kafka custom instrumentation for producers and consumers
* History app-level custom instrumentation for requests towards the Cassandra HistoryDB

Since OpenTelemetry is rather new in .NET there are fewer out-of-the box instrumentation libraries. This is the reason for the custom instrumentation implementation. Because of that it takes some shortcuts. E.g. it doesn't propagate the `Baggage` for now. OpenTelemetry specification lists a number of database, rpc, messaging (general) and Cassandra, gRPC, Kafka (specific) attributes that can be added to a span. The custom instrumentation implementation doesn't go into the detail of adding **all** of them, only the most important ones.

The API used is the .NET one which is recommended for now, instead of the OpenTelemetry wrapper. Since it is a bleeding-edge tech that may change. The complexity of the instrumentation implementation is a result of specific details how spans are started/stopped, whether or not a span object is created based on sampling and how spans are tracked via a static execution context. These are interwined with each other and propagating spans, using simultaneously running worker threads, probability-based sampling made doing it a challenge.

## Sampling

For `Debug` all traces are sampled. For `Release` this is true for 10% of them. Additionally, when health-check APIs are added they should be excluded as well. A configuration based approach is taken which would allow higher degree of customization.

## Exporting

Jaeger is used in order to show traces and spans since an exporter for it is provided out-of-the-box. Additionally a few other ones could be used as well. The development environment uses the all-in-one Jaeger container which stores traces only in memory and that is enough. For production though a full deployment of Jaeger and all its components would have to be implemented.

# Log aggregation

EFK stack is utilized to aggregate, store, view, search all logs and setup alerts.

## Aggregation

In Debug/Development mode servers are configured to write logs directly to ElasticSearch. Serilog provides a specialized formatter using the JSON fields which ElasticSearch understands.

In Release/Production mode the servers simply write logs to `stdout`. The same Serilog formatter for ElasticSearch is used. Docker logging driver for Fluentd is configured in the docker-compose file. It enables sending the logs to a Fluentd container. It matches them by tag, parses them by using only the `log` JSON field and sends that data to ElasticSearch.

Both modes are configured to create indexes with different names because there is a slight difference in the fields. Also it makes it easier to know where the logs came from.

## Storage, view, alerts

Once the logs are in the appropriate indexes in ElasticSearch Kibana can be used in order to search by whatever field is needed. Log streaming and alerts can also be used.
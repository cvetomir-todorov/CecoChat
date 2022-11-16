# Observability

## Distributed tracing

OpenTelemetry is utilized to make flow of communication and data exchange observable and help resolve production issues.

### Resources

Each service is represented as a separate resource with namespace, name and version.

### Instrumentation

The following instrumentation is used:
* Out-of-the-box
  - ASP.NET Core (with gRPC support for incoming requests) which is out-of-the box
  - gRPC client instrumentation
* Custom
  - gRPC instrumentation for server streaming which wasn't present/working
  - Kafka instrumentation for producers and consumers
  - History and state app-level instrumentation for requests towards Cassandra - HistoryDB, StateDB

Since OpenTelemetry is rather new in .NET there are fewer out-of-the box instrumentation libraries. This is the reason for the custom instrumentation implementation. Because of that it takes some shortcuts. E.g. it doesn't propagate the `Baggage` for now. OpenTelemetry specification lists a number of database, rpc, messaging (general) and Cassandra, gRPC, Kafka (specific) attributes that can be added to a span. The custom instrumentation implementation doesn't go into the detail of adding **all** of them, only the most important ones.

The API used is the .NET one which is recommended for now, instead of the OpenTelemetry wrapper. Since it is a bleeding-edge tech that may change. The complexity of the instrumentation implementation is a result of specific details how spans are started/stopped, whether or not a span object is created based on sampling and how spans are tracked via a static execution context. These are intertwined with each other and propagating spans, using simultaneously running worker threads, probability-based sampling made doing it a challenge.

### Sampling

For `Debug` all traces are sampled. For `Release` this is true for 10% of them. The `/metrics` endpoint are excluded. Additionally, when health-check APIs are added they should be excluded as well. A configuration based approach is taken which allows a high degree of customization both during development and deployment.

### Exporting

Jaeger is used in order to show traces and spans since an exporter for it is provided out-of-the-box. Additionally a few other ones could be used as well. The development environment uses the all-in-one Jaeger container which stores traces only in memory and that is enough. For production though a full deployment of Jaeger and all its components would have to be implemented.

## Log aggregation

EFK stack is utilized to aggregate, store, view, search all logs and setup alerts.

### Aggregation

In Debug/Development mode services are configured to write logs directly to ElasticSearch. Serilog provides a specialized formatter using the JSON fields which ElasticSearch understands.

In Release/Production mode the services simply write logs to `stdout`. The same Serilog formatter for ElasticSearch is used. Docker logging driver for Fluentd is configured in the docker-compose file. It enables sending the logs to a Fluentd container. It matches them by tag, parses them by using only the `log` JSON field and sends that data to ElasticSearch.

Both modes are configured to create indexes with different names because there is a slight difference in the fields. Also it makes it easier to know where the logs came from.

### Storage, view, alerts

Once the logs are in the appropriate indexes in ElasticSearch Kibana can be used in order to search by whatever field is needed. Log streaming and alerts can also be used.

## Metrics

Since OpenTelemetry is maturing, its SDK and API for metrics is used with the appropriate exporters. For now this is Prometheus, but in the future InfluxDB or another database could be preferred. 

The following metrics are exported from the servers:
* Protocol level
  - HTTP request duration histogram
  - Cassandra query duration histogram
* Application level
  - Messages
    - Received count
    - Processed count
  - Online clients count

## Monitoring

* Collection
  - OpenTelemetry is used to collect metrics and traces via push/pull methods
  - Prometheus is used to scrape metrics via a `/metrics` endpoint
  - Jaeger is used to collect traces pushed to it
* Visualization
  - Grafana adds data sources for the respective collectors
  - It presents dashboards with panels to visualize the different metrics being provided to it

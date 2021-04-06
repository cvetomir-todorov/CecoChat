# Configurability

## Static

Static app configuration is stored in `appsettings.json` files as is typical for .NET applications. For some servers it is overriden for the `Development` environment via the `appsettings.ENVIRONMENT.json` approach.

Static logging configuration also uses the same approach with a main file and an environment-specific one. It is in separate `logging.json` and `logging.ENVIRONMENT.json` files based on Serilog logger configuration format.

The [CecoChat docker-compose file](../run/cecochat.yml) uses environment variables with specific prefixes in order to push static configuration values to the servers at start-up. [ASP.NET documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#environment-variables) describes how to name the environment variables in order to override both ASP.NET and app-specific values.

## Dynamic

Some parts of the configuration are designed to be changed while it is running. These parts are stored in Redis and read at start-up. The [prepare script](../run/prepare/redis-create-configuration.sh) inputs the partitioning and history configuration values which need to be present and valid at all times. The configuration can be changed manually using the `redis-cli` command after attaching to the Redis container via `docker exec -it cecochat-redis1 bash`. Each server that is using dynamic configuration outputs at start-up the key names it reads and PUB/SUB channels which it subscribes to in order to get notified about changes. After the configuration is changed a dummy message needs to be sent to one of those PUB/SUB channels. The servers validate the new configuration values and may reject the change. The validation output provides descriptive information which can be used to correct the values.

In the future there may be a web-based Configurator which uses a Redis client and would allow changing the configuration more easily.

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
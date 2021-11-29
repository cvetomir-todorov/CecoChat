# Technologies

## Client communication

* Clients use HTTP to communicate with the BFF service
* Clients use gRPC to communication with the Messaging service
* If web browser support is needed it is worth considering replacing gRPC with HTTP and WebSockets because HTTP/2 doesn't allow it to be used easily in the browser
* [gRPC-web](https://github.com/grpc/grpc-web) uses an [Envoy proxy](https://www.envoyproxy.io/) which supports server-side streaming but that would be expensive to implement

## Service synchronous communication

Services communicate synchronously with each other via gRPC:
* open-source software backed by Google
* supports multiple languages
* lightweight and has good performance
* based on HTTP/2 which allows for both inter-operability and optimizations from the protocol

## Service asynchronous communication

Services communicate asynchronously via the PUB/SUB backplane which is based on Apache Kafka:
* open-source software backed by Confluent
* a linearly scalable message broker
* enables a superb throughput due to its balanced distribution of partition leadership throughout the cluster
* fault-tolerant and persists messages with customizable durability
* can be tuned for either low latency, high-throughput or something in-between
* allows different consumer groups each of which can process messages independently from each other
* has a pull model which allows consumers to process messages at their own rate
* a good solution for an event log, especially when processing a single message is fast
* has some known operability issues with partition redistribution among a consumer group
* relies on ZooKeeper as an additional element in the infrastructure

## State and History databases

State and history are based on Apache Cassandra:
* open-source software backed by DataStax
* linearly scalable
* distributed with auto-partitioning and auto-replication
* supports multi data-center auto-replication
* suitable for random small fast writes and reads
* suitable for random range queries
* suitable for time series
* eliminates the need of caching
* allows precise control over the consistency used for writes and reads
* ensures data consistency via hinted hand-offs and read repairs
* needs regular anti-entropy repairs which are CPU-bound

## Configuration DB

Configuration is based on Redis:
* open-source software
* lightweight and performant
* easy to use and manage
* has a built-in PUB/SUB mechanism
* should be replaced by a more reliable technology in the future, e.g. a relational DB for storage and Kafka for notifications

## Services

The services use ASP.NET/.NET 5 which are the heirs of ASP.NET/.NET Core:
* open-source software backed by Microsoft
* very mature, feature-rich, lots of tools
* widely-used with a big community
* well supported
* should be updated to ASP.NET/.NET 6 and regularly to the expected upcoming next versions

## Observability

* distributed tracing is based on OpenTelemetry and Jaeger is configured for viewing traces and spans
* log aggregation is based on the EFK stack consisting of ElasticSearch, Fluentd and Kibana
* monitoring and metrics - TBD

## Deployment

* containerization relies on Docker for its maturity, popularity, tooling and integration
* orchestration - TBD, probably Kubernetes
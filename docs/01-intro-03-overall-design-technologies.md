# Overall design

![Overall design](images/cecochat-01-overall.png)

* Clients initially connect to a Backend for frontend (BFF) service in order to create their session
* Clients acquire user chats and chat history from the State and History services via the BFF
* Clients connect to Messaging service in order to chat with each other
* Messaging servers exchange data between each other indirectly using a PUB/SUB backplane
* PUB/SUB backplane distributes the traffic between Messaging servers
* ID Gen service is used to generate Snowflake message IDs
* State service transform the messages from the PUB/SUB backplane into State database which is the source of truth 
* History service transform messages and reactions from the PUB/SUB backplane into a History database which is the source of truth
* Splitting the different responsibilities between separate services allows for independent scaling
* The services use dynamic configuration which is updated centrally
* Observability is achieved via distributed tracing, log aggregation and monitoring with metrics
* Deployment infrastructure takes care of failover, growth/shrinkage of the different services based on load and predictive analytics

All the diagrams are in the [diagrams](diagrams/) folder and [draw.io](https://app.diagrams.net/) is needed in order to view them. From the `Help` item in the menu a desktop tool could be downloaded, if preferred. Currently this is the [link with the releases](https://github.com/jgraph/drawio-desktop/releases).

# Technologies

## Client communication

* Clients use HTTP to communicate with the BFF service
* Client use gRPC to communication with the Messaging service
* If web browser support is needed it is worth considering replacing gRPC with HTTP and WebSockets because HTTP/2 doesn't allow it to be used easily in the browser
* [gRPC-web](https://github.com/grpc/grpc-web) uses an [Envoy proxy](https://www.envoyproxy.io/) which supports server-side streaming but that would be expensive to implement

## Service synchronous communication

Servers communicate synchronously with each other via gRPC:
* open-source software backed by Google
* supports multiple languages
* lightweight and has good performance
* based on HTTP/2 which allows for both inter-operability and optimizations from the protocol

## PUB/SUB backplane

PUB/SUB backplane is based on Apache Kafka:
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

## Application servers

The application servers use ASP.NET/.NET 5 which are the heirs of ASP.NET Core and .NET Core:
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
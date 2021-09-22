# Overall design

![Overall design](images/cecochat-01-overall.png)

Clients connect to messaging servers in order to chat. Messaging servers communicate with each other indirectly using a PUB/SUB backplane. The PUB/SUB backplane also acts like an event log. Materialize servers transform messages from the event log into a history database which is the source of truth. The history is available for querying by the clients via history servers. Clients obtain the addresses for the messaging and history servers from a connect server. ID Gen servers are used to generate message IDs in a distributed and scalable way. The messaging, history, connect servers use dynamic configuration which is updated centrally. Splitting the different responsibilities between separate servers allows for independent scaling. A deployment infrastructure takes care of failover, growth/shrinkage of the different server sets based on load and predictive analytics.

All the diagrams are in the [diagrams](diagrams/) folder and [draw.io](https://app.diagrams.net/) is needed in order to view them. From the `Help` item in the menu a desktop tool could be downloaded, if preferred. Currently this is the [link with the releases](https://github.com/jgraph/drawio-desktop/releases).

# Technologies

## Client communication

Clients use HTTP when they need to find out where to connect. After that gRPC is utilized in order to obtain history and exchange messages:
* open-source software backed by Google
* supports multiple languages, which is important for the variety of clients
* lightweight and has good performance
* based on HTTP/2 which allows for both inter-operability and optimizations from the protocol
* uses full-duplex communication which is possible in mobile and desktop clients
* usage of HTTP/2 doesn't allow it to be used easily in the browser but [gRPC-web](https://github.com/grpc/grpc-web) uses an [Envoy proxy](https://www.envoyproxy.io/) which supports server-side streaming.

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

## History DB

History is based on Apache Cassandra:
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

## Application servers

The application servers use ASP.NET and .NET 5 which are the heirs of ASP.NET Core and .NET Core:
* open-source software backed by Microsoft
* very mature, feature-rich, lots of tools
* widely-used with a big community
* well supported

## Operations

* Containerization relies on Docker for its maturity, popularity, tooling and integration
* Distributed tracing is based on OpenTelemetry and Jaeger is configured for viewing traces and spans
* Log aggregation is based on the EFK stack consisting of ElasticSearch, Fluentd and Kibana
* Deployment infrastructure - TBD

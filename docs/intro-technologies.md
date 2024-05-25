# Technologies

The technologies chosen are [cloud-agnostic](https://codersociety.com/blog/articles/cloud-agnostic) which makes the solution independent of a specific cloud provider.

* Integration - Kafka, gRPC, WebSocket, HTTP
* Data storage - Yugabyte, Cassandra, MinIO, Redis
* Operations - OpenTelemetry, Docker, Kubernetes
* Services - .NET, ASP.NET, SignalR, EF Core
* Libraries - Autofac, Serilog, FluentValidation, AutoMapper, Polly, IdGen
* Automated testing - NUnit, FluentAssertions, Coverlet

![Technologies](images/cecochat-technologies.png)

# Client communication

![WebSocket](tech-images/websocket.webp)
![MessagePack](tech-images/messagepack.webp)
![HTTP](tech-images/http.png)
![JSON](tech-images/json.png)
![Swagger](tech-images/swagger.png)

* Clients use WebSocket transport with MessagePack message format to communicate with the Messaging service
* Clients use HTTP transport with JSON message format to communicate with the BFF service

# Service synchronous communication

![gRPC](tech-images/grpc.png)
![Protocol Buffers](tech-images/protocol-buffers.png)

Services communicate synchronously with each other via gRPC:
* open-source software backed by Google
* supports multiple languages
* lightweight and has good performance
* based on HTTP/2 which allows for both inter-operability and optimizations from the protocol

# Service asynchronous communication

![Kafka](tech-images/kafka.png)
![Protocol buffers](tech-images/protocol-buffers.png)

Services communicate asynchronously via the PUB/SUB backplane which is based on Kafka:
* open-source software backed by Confluent
* a linearly scalable message broker
* enables a superb throughput due to its balanced distribution of partition leadership throughout the cluster
* fault-tolerant and persists messages with customizable durability
* can be tuned for either low latency, high-throughput or something in-between
* allows different consumer groups each of which can process messages independently of each other
* has a pull model which allows consumers to process messages at their own rate
* a good solution for an event log, especially when processing a single message is fast
* has some known operability issues with partition redistribution among a consumer group
* relies on ZooKeeper as an additional element in the infrastructure

# User database

![Yugabyte](tech-images/yugabyte.png)
![Postgres](tech-images/postgres.webp)

User database is based on Yugabyte DB

* distributed SQL database
* based on Postgres and driver-compatible with it
* highly-available with built-in data replication and follower reads
* horizontally scalable
* supports partitioning (sharding) by hash or range
* handles distributed transactions in a scalable and resilient way with optimistic concurrency
* supports traditional SQL indexes and constraints
* has different multi-region deployment modes
* supports change data capture (CDC)
* observable with integration for popular existing technologies

# User cache

![Redis](tech-images/redis.png)

User cache is based on Redis:
* open-source software
* lightweight and performant
* easy to use and manage

# Chats database

![Cassandra](tech-images/cassandra.png)

Chats database is based on Cassandra:
* open-source software backed by DataStax
* linearly scalable
* distributed with auto-partitioning and auto-replication
* supports multi data-center auto-replication
* suitable for random small fast writes and reads
* suitable for random range queries
* suitable for time series
* eliminates the need of caching
* allows precise control over the consistency used for writes and reads
* ensures data consistency via hinted handoffs and read repairs
* needs regular anti-entropy repairs which are CPU-bound

# File storage

![Minio](tech-images/minio.webp)

File storage is based on MinIO:
* object store - stores both file and metadata
* horizontally scalable
* supports multi-site replication
* has global identity and access management
* supports bucket encryption
* supports immutability and versioning of objects
* provides tools for monitoring
* provides data-management interfaces
* HTTP-accessible with a native client out of the box
* AWS S3-compatible

# Dynamic configuration

![Yugabyte](tech-images/yugabyte.png)
![Postgres](tech-images/postgres.webp)
![Kafka](tech-images/kafka.png)
![gRPC](tech-images/grpc.png)
![Protocol Buffers](tech-images/protocol-buffers.png)

Dynamic configuration is based on multiple technologies:
* storage is based on Yugabyte DB which is Postgres driver-compatible
* services are notified for configuration changes via dedicated Kafka topic
* services acquire configuration both initially and when changed using gRPC
* both Kafka and gRPC data format is Protocol Buffers

# Observability

![OpenTelemetry](tech-images/open-telemetry.png)
![Jaeger](tech-images/jaeger.png)
![ElasticSearch](tech-images/elasticsearch.png)
![Kibana](tech-images/kibana.png)
![Prometheus](tech-images/prometheus.png)
![Grafana](tech-images/grafana.png)

* Distributed tracing is based on OpenTelemetry and Jaeger
* Log aggregation is based on OpenTelemetry, ElasticSearch and Kibana
* Metrics is based on OpenTelemetry, Prometheus and Grafana

# Deployment

![Docker](tech-images/docker.png)
![Kubernetes](tech-images/kubernetes.webp)
![Helm](tech-images/helm.webp)

* Containerization relies on Docker for its maturity, popularity, tooling and integration
* Orchestration is based on Kubernetes and Helm for its power, maturity, popularity, tooling
* infrastructure as code - TBD, probably Terraform

# Services

![.NET](tech-images/dotnet.png)
![ASP.NET](tech-images/aspnet.png)
![EFCore](tech-images/efcore.png)
![SignalR](tech-images/signalr.webp)

The services are based on .NET, ASP.NET, SignalR and Entity Framework Core:
* open-source software backed by Microsoft
* very mature, feature-rich, lots of tools
* widely-used with a big community
* well-supported

![Autofac](tech-images/autofac.png)
![Serilog](tech-images/serilog.png)
![FluentValidation](tech-images/fluent-validation.png)
![AutoMapper](tech-images/automapper.webp)
![Polly](tech-images/polly.png)
![Refit](tech-images/refit.png)

Additional libraries which are all open-source and created, supported and developed by the community:
* Autofac is the most popular DI container
* Serilog is one of the most popular logging frameworks
* FluentValidation is the most popular data validation library
* AutoMapper is the most popular mapping library
* Polly is a popular HTTP client policies library
* IdGen is used to generate Snowflake IDs
* Refit is one of the most popular REST clients

![NUnit](tech-images/nunit.jpg)
![FluentAssertions](tech-images/fluent-assertions.png)
![Coverlet](tech-images/coverlet.png)

Automated testing is done using open-source libraries, supported and developed by the community:
* NUnit is one of the 2 most popular unit testing frameworks
* Fluent Assertions is the most mature assertion framework
* Coverlet is a powerful code coverage toolset

# Tools

![Ubuntu](tech-images/ubuntu.webp)
![Git](tech-images/git.webp)
![JetBrainsRider](tech-images/jetbrains-rider.png)
![Minikube](tech-images/minikube.webp)

* Ubuntu is an excellent choice for power users doing development
* Git is a non-negotiable VCS for development at the time of writing
* JetBrains Rider is the second most used .NET IDE
* Minikube is one of the most popular Kubernetes local clusters

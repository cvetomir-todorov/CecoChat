[Chat engine](docs/intro-capabilities.md) for millions of users.

[System design](docs/intro-design.md) based on microservice architecture. [Implementation](source/) based on cloud agnostic technologies - Kafka, YugabyteDB, Cassandra, HTTP, gRPC, Redis, .NET 6, ASP.NET. Dynamically and statically [configurable](docs/design-configuration.md) microservices. [Observable](docs/design-observability.md) environment based on OpenTelemetry, Jaeger, Prometheus, Grafana, ElasticSearch, Fluentd, Kibana. Containerized components based on Docker which can be [ran locally](docs/dev-run-locally.md).

Check out [what next](docs/what-next.md) needs to be implemented. I appreciate all comments so feel free to use the `Discussions` tab.

# Code

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=cvetomir-todorov_CecoChat&metric=alert_status)](https://sonarcloud.io/dashboard?id=cvetomir-todorov_CecoChat)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=cvetomir-todorov_CecoChat&metric=sqale_rating)](https://sonarcloud.io/dashboard?id=cvetomir-todorov_CecoChat)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=cvetomir-todorov_CecoChat&metric=reliability_rating)](https://sonarcloud.io/dashboard?id=cvetomir-todorov_CecoChat)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=cvetomir-todorov_CecoChat&metric=security_rating)](https://sonarcloud.io/dashboard?id=cvetomir-todorov_CecoChat)

[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=cvetomir-todorov_CecoChat&metric=ncloc)](https://sonarcloud.io/dashboard?id=cvetomir-todorov_CecoChat)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=cvetomir-todorov_CecoChat&metric=bugs)](https://sonarcloud.io/dashboard?id=cvetomir-todorov_CecoChat)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=cvetomir-todorov_CecoChat&metric=vulnerabilities)](https://sonarcloud.io/dashboard?id=cvetomir-todorov_CecoChat)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=cvetomir-todorov_CecoChat&metric=code_smells)](https://sonarcloud.io/dashboard?id=cvetomir-todorov_CecoChat)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=cvetomir-todorov_CecoChat&metric=duplicated_lines_density)](https://sonarcloud.io/dashboard?id=cvetomir-todorov_CecoChat)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=cvetomir-todorov_CecoChat&metric=sqale_index)](https://sonarcloud.io/dashboard?id=cvetomir-todorov_CecoChat)

# Technologies

![Kafka](docs/tech-images/kafka.png)
![gRPC](docs/tech-images/grpc.png)
![Protocol buffers](docs/tech-images/protocol-buffers.png)
![WebSocket](docs/tech-images/websocket.webp)
![MessagePack](docs/tech-images/messagepack.webp)
![HTTP](docs/tech-images/http.png)
![JSON](docs/tech-images/json.png)
![Swagger](docs/tech-images/swagger.png)

![Yugabyte](docs/tech-images/yugabyte.png)
![Postgres](docs/tech-images/postgres.webp)
![Cassandra](docs/tech-images/cassandra.png)
![Redis](docs/tech-images/redis.png)

![.NET](docs/tech-images/dotnet.png)
![ASP.NET](docs/tech-images/aspnet.png)
![EFCore](docs/tech-images/efcore.png)
![SignalR](docs/tech-images/signalr.webp)

![Autofac](docs/tech-images/autofac.png)
![Serilog](docs/tech-images/serilog.png)
![AutoMapper](docs/tech-images/automapper.webp)
![FluentValidation](docs/tech-images/fluent-validation.png)
![Polly](docs/tech-images/polly.png)
![Refit](docs/tech-images/refit.png)

![OpenTelemetry](docs/tech-images/open-telemetry.png)
![Jaeger](docs/tech-images/jaeger.png)
![Prometheus](docs/tech-images/prometheus.png)
![Grafana](docs/tech-images/grafana.png)
![ElasticSearch](docs/tech-images/elasticsearch.png)
![Fluentd](docs/tech-images/fluentd.png)
![Kibana](docs/tech-images/kibana.png)

![Docker](docs/tech-images/docker.png)

# Documentation

* Intro
  - [Capabilities](docs/intro-capabilities.md)
  - [Overall design](docs/intro-design.md)
  - [Technologies](docs/intro-technologies.md)
* Research
  - [Concurrent connections limit](docs/research-connection-limit.md)
  - [Calculations](docs/research-calculations.md)
  - [Main problems](docs/research-main-problems.md)
* Design
  - [Messaging](docs/design-messaging.md)
  - [User chats, chat history](docs/design-state-history.md)
  - [Clients](docs/design-clients.md)
  - [Configuration](docs/design-configuration.md)
  - [Observability](docs/design-observability.md)
  - [Deployment](docs/design-deployment.md)
* Development
  - [CI pipeline](docs/dev-ci.md)
  - [Run locally](docs/dev-run-locally.md)
* [What next](docs/what-next.md)

# Design

### Overall
![Design](docs/images/cecochat-01-overall.png)

### Technologies
![Technologies](docs/images/cecochat-08-technologies.png)

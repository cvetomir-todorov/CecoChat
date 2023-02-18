Chat for millions of users with [limited-for-now capabilities](docs/intro-main.md).

[System design](docs/intro-design.md) and [partial implementation](source/) based on Kafka, Cassandra, HTTP, gRPC, Redis, .NET 6, ASP.NET. [Configurable](docs/design-configuration.md) and [observable](docs/design-observability.md) environment based on OpenTelemetry, Jaeger, Prometheus, Grafana, ElasticSearch, Fluentd, Kibana. Containerized components based on Docker which can be [ran locally](docs/infrastructure-main.md#Run-locally).

I appreciate all comments so feel free to use the `Discussions` tab.

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

### Integration
![Kafka](docs/tech-images/kafka.png)
![gRPC](docs/tech-images/grpc.png)
![Protocol buffers](docs/tech-images/protocol-buffers.png)
![HTTP](docs/tech-images/http.png)
![JSON](docs/tech-images/json.png)
![Swagger](docs/tech-images/swagger.png)

### Data
![Cassandra](docs/tech-images/cassandra.png)
![Yugabyte](docs/tech-images/yugabyte.png)
![Postgres](docs/tech-images/postgres.webp)
![Redis](docs/tech-images/redis.png)

### Internal implementation
![.NET](docs/tech-images/dotnet.png)
![ASP.NET](docs/tech-images/aspnet.png)
![EFCore](docs/tech-images/efcore.png)
![Autofac](docs/tech-images/autofac.png)
![Serilog](docs/tech-images/serilog.png)
![AutoMapper](docs/tech-images/automapper.webp)
![FluentValidation](docs/tech-images/fluent-validation.png)
![Refit](docs/tech-images/refit.png)
![Polly](docs/tech-images/polly.png)

### Observability
![OpenTelemetry](docs/tech-images/open-telemetry.png)
![Jaeger](docs/tech-images/jaeger.png)
![Prometheus](docs/tech-images/prometheus.png)
![Grafana](docs/tech-images/grafana.png)
![ElasticSearch](docs/tech-images/elasticsearch.png)
![Fluentd](docs/tech-images/fluentd.png)
![Kibana](docs/tech-images/kibana.png)

### Deployment
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
* Infrastructure
  - [CI pipeline](docs/infrastructure-main.md#CI-pipeline)
  - [Run locally](docs/infrastructure-main.md#Run-locally)
* [What next](docs/what-next.md)

# Design

![Design](docs/images/cecochat-01-overall.png)

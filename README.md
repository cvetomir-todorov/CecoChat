[System design](docs/design-overall.md) of a chat for 10-100 millions active users. [Partial implementation](source/) based on Kafka, Cassandra, gRPC, Redis, .NET 5, ASP.NET. [Configurable](docs/design-configuration.md) and [partially-observable](docs/design-observability.md) environment with containerized components which can be [ran locally](docs/infrastructure-main.md#Run-locally) and is based on Docker, OpenTelemetry, Fluentd, ElasticSearch, Kibana, Jaeger. I appreciate all comments so feel free to use the `Discussions` tab.

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
![Cassandra](docs/tech-images/cassandra.png)
![gRPC](docs/tech-images/grpc.png)
![Redis](docs/tech-images/redis.png)
![Protocol buffers](docs/tech-images/protocol-buffers.png)
![Swagger](docs/tech-images/swagger.png)

![Docker](docs/tech-images/docker.png)
![OpenTelemetry](docs/tech-images/open-telemetry.png)
![Fluentd](docs/tech-images/fluentd.png)
![ElasticSearch](docs/tech-images/elasticsearch.png)
![Kibana](docs/tech-images/kibana.png)
![Jaeger](docs/tech-images/jaeger.png)

![.NET](docs/tech-images/dotnet.png)
![ASP.NET](docs/tech-images/aspnet.png)
![Autofac](docs/tech-images/autofac.png)
![Serilog](docs/tech-images/serilog.png)
![Polly](docs/tech-images/polly.png)
![FluentValidation](docs/tech-images/fluent-validation.png)

# Documentation

* Intro
  - [Capabilities](docs/intro-main.md#Capabilities)
  - [Concurrent connections benchmark](docs/intro-main.md#Concurrent-connections-benchmark)
  - [Back of the envelope calculations](docs/intro-back-of-the-envelope.md)
* Design
  - [Overall design](docs/design-overall.md)
  - [Technologies](docs/design-technologies.md)
  - [Main problems](docs/design-main-problems.md)
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

System design and partial implementation of a chat for 10-100 millions active users based on Kafka, Cassandra, gRPC, Redis, Docker, .NET 5, ASP.NET. I would appreciate any comments so feel free to use the `Discussions` tab.

![Kafka](docs/tech-images/kafka.png)
![Cassandra](docs/tech-images/cassandra.png)
![gRPC](docs/tech-images/grpc.png)
![Redis](docs/tech-images/redis.png)
![Docker](docs/tech-images/docker.png)
![.NET](docs/tech-images/dotnet.png)

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

# Design

![Overall design](docs/images/cecochat-01-overall.png)

# Documentation

* Intro
  - [Capabilities](docs/01-intro-01-main.md#Capabilities)
  - [Concurrent connections benchmark](docs/01-intro-01-main.md#Concurrent-connections-benchmark)
  - [Back of the envelope calculations](docs/01-intro-01-main.md#Back-of-the-envelope-calculations)
  - [Overall design, technologies](docs/01-intro-03-overall-design-technologies.md)
* Design
  - [Send receive approach](docs/02-design-01-approach.md)
  - [Send, receive, multiple clients](docs/02-design-02-send-receive.md)
  - [History, clients](docs/02-design-03-history-clients.md)
  - [Configuration, failover](docs/02-design-04-configuration-failover.md)
* Infrastructure
  - [CI/CD](docs/03-infrastructure-01-main.md#CICD)
  - [Run](docs/03-infrastructure-01-main.md#Run)
* [What next](docs/04-what-next.md)

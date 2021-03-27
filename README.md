System design and partial implementation of a chat for 10-100 millions active users based on Kafka, Cassandra, gRPC, Redis, Docker, .NET 5, ASP.NET.

![Kafka](docs/tech-images/kafka.png)
![Cassandra](docs/tech-images/cassandra.png)
![gRPC](docs/tech-images/grpc.png)
![Redis](docs/tech-images/redis.png)
![Docker](docs/tech-images/docker.png)
![.NET](docs/tech-images/dotnet.png)

I would appreciate any comments so feel free to use the `Discussions` tab on the Git repo.

P.S. The solution is named after my short name.

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
  - [Capabilities](docs\01-intro-01-main.md#Capabilities)
  - [Concurrent connections benchmark](docs\01-intro-01-main.md#Concurrent-connections-benchmark)
  - [Back of the envelope calculations](docs\01-intro-01-main.md#Back-of-the-envelope-calculations)
  - [Overall design](docs\01-intro-03-overall-design-technologies.md#Overall-design)
  - [Technologies](docs\01-intro-03-overall-design-technologies.md#Technologies)
* Design
  - [Send receive approach](docs\02-design-01-approach.md)
  - [Send](docs\02-design-02-send-receive.md#Send)
  - [Receive](docs\02-design-02-send-receive.md#Receive)
  - [Sender with multiple clients](docs\02-design-02-send-receive.md#Sender-with-multiple-clients)
  - [History](docs\02-design-03-history-clients.md#History)
  - [Clients](docs\02-design-03-history-clients.md#Clients)
  - [Configuration](docs\02-design-04-configuration-failover.md#Configuration)
  - [Failover](docs\02-design-04-configuration-failover.md#Failover)
* Infrastructure
  - [CI/CD](docs/03-infrastructure-01-main.md#CI/CD)
  - [Run](docs/03-infrastructure-01-main.md#Run)
* [What next](docs/04-what-next.md)

# Why

I decided to take on the challenge to design a globally-scalable chat like WhatsApp and Facebook Messenger. Based on [statistics](https://www.statista.com/statistics/258749/most-popular-global-mobile-messenger-apps/) the montly active users are 2.0 bln for WhatsApp and 1.3 bln for Facebook Messenger. At that scale I decided to start a bit smaller. A good first step was to design a system that would be able to handle a smaller number of active users which are directly connected to it. Let's call it a cell. After that I would need to design how multiple cells placed in different geographic locations would communicate with each other. I certainly don't have the infrastructure to validate the design and the implementation. But I used the challenge to think at a large scale and to learn a few new technologies and approaches along the way.

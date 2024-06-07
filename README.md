[Chat engine](docs/intro-capabilities.md) designed for millions of users

* Messaging in real-time
  - Send and receive messages
  - Send files - images, text, PDF
  - React/unreact with emojis
  - Multiple clients for the same user
* Chats
  - Notifications when a message has been processed
  - Indication for new messages
  - Review history at a random point in time
* User
  - Register, authenticate
  - Change password, edit trivial profile data
  - Store small user files - images, text, PDF
* Other users
  - Search other users by name
  - Connect with other users - invite/accept/cancel/remove
  - Profiles with static full/public-only data

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

# Technology

* [System design](docs/intro-design.md) based on [microservice architecture](https://microservices.io/)
* [Implementation](source) based on [cloud-agnostic technologies](docs/intro-technologies.md)
* [Configurable](docs/design-configuration.md), [observable](docs/design-observability.md), [containerized and runnable](docs/dev-run-prerequisites.md), [deployable](docs/design-deployment.md) local environment
* [Main technologies](docs/intro-technologies.md)
  - Integration - Kafka, gRPC, WebSocket, HTTP
  - Data storage - Yugabyte, Cassandra, MinIO, Redis
  - Operations - OpenTelemetry, Docker, Kubernetes
  - Services - .NET, ASP.NET, SignalR, EF Core
  - Libraries - Autofac, Serilog, FluentValidation, AutoMapper, Polly, IdGen
  - Automated testing - NUnit, Testcontainers, FluentAssertions, Coverlet

# Design

![Design](docs/images/cecochat-overall.png)

# Documentation

* Intro
  - [Capabilities](docs/intro-capabilities.md)
  - [Overall design](docs/intro-design.md)
  - [Technologies](docs/intro-technologies.md)
* Research
  - [Concurrent connections limit](docs/research-connection-limit.md)
  - [Calculations](docs/research-calculations.md)
  - [Messaging traffic](docs/research-messaging-traffic.md)
  - [Message IDs](docs/research-message-ids.md)
  - [Reliable messaging and consistency](docs/research-reliable-messaging-consistency.md)
* Design
  - [Messaging](docs/design-messaging.md)
  - [Chats](docs/design-chats.md)
  - [User - profiles, connections, files](docs/design-users.md)
  - [Clients](docs/design-clients.md)
  - [Configuration - static and dynamic](docs/design-configuration.md)
  - [Observability](docs/design-observability.md)
  - [Deployment](docs/design-deployment.md)
* Development
  - [Local run prerequisites](docs/dev-run-prerequisites.md)
  - [Local run in Docker](docs/dev-run-docker.md)
  - [Local run in Minikube](docs/dev-run-minikube.md)
  - [Development process](docs/dev-process.md)
* [Load test using 2 machines](docs/load-test.md)
* [What next](docs/what-next.md)

# Ban

Any use of the project for training large language models, generative AI, or any other similar tools in the past/present/future is done without our permission.

# License

* [Server Side Public License (SSPL)](https://www.mongodb.com/licensing/server-side-public-license)
* [FAQ about SSPL](https://www.mongodb.com/licensing/server-side-public-license/faq)
* [Comparison of AGPL to SSPL](https://webassets.mongodb.com/_com_assets/legal/SSPL-compared-to-AGPL.pdf)

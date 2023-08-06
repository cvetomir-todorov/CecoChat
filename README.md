[Chat engine](docs/intro-capabilities.md) for millions of users

* Messaging in real-time
  - Send and receive messages, (un)react with emojis
  - Notifications when a message has been processed
  - Multiple clients for the same user
* Chats
  - Indication for new messages
  - Review history at a random point in time
* Users
  - Registration and authentication
  - Profiles with full and public-only data

Check out [what next](docs/what-next.md) needs to be implemented. I appreciate all comments so feel free to use the `Discussions` tab.

# Technology

* [System design](docs/intro-design.md) based on [microservice architecture](https://microservices.io/)
* [Implementation](source) based on [cloud agnostic technologies](docs/intro-technologies.md)
* [Configurable](docs/design-configuration.md), [observable](docs/design-observability.md), [containerized and runnable](docs/dev-run-prerequisites.md), [deployable](docs/design-deployment.md) local environment

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
  - [User chats, chat history](docs/design-state-history.md)
  - [Clients](docs/design-clients.md)
  - [Configuration](docs/design-configuration.md)
  - [Observability](docs/design-observability.md)
  - [Deployment](docs/design-deployment.md)
* Development
  - [Local run prerequisites](docs/dev-run-prerequisites.md)
  - [Local run in Docker](docs/dev-run-docker.md)
  - [Local run in Minikube](docs/dev-run-minikube.md)
  - [CI pipeline](docs/dev-ci.md)
* [Load test using 2 machines](docs/load-test.md)
* [What next](docs/what-next.md)

# Design

![Design](docs/images/cecochat-01-overall.png)

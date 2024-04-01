# Internal design

Most services have the following layers and/or libraries associated with them:
* Service contracts
* Service data access
* Service host, endpoints, producers, consumers
* Service client

.NET projects are grouped into solution folders depending on the functionality:
* `Components` contains the projects for each piece of the system design
  * `Backplane` contains the projects related to the backplane
  * `BFF` contains the projects related to the BFF service
  * `Chats` contains the projects related to the Chats service
  * `Config` contains the projects related to the Config service
  * `IdGen` contains the projects related to the ID Gen service
  * `Messaging` contains the projects related to the Messaging service
  * `User` contains the projects related to the User service
* `ClientApp` contains the console client used for manual testing and the load testing app
* `Shared` contains project-specific functionality, reusable across services
* `Common` contains technology-oriented functionality, which could also be reusable in a different project

# Running

* Each service contains a customized `launchSettings.json` file which should be used in order to start it locally from the IDE
* All services depend on the Config service which needs to be started prior to the others, so they obtain the configuration elements they need
* The messaging service has 2 profiles in order to simulate realistic communication between user clients connected to different instances
* Ports for each service are defined in [server addresses file](../source/server-addresses.txt)

# CI pipeline

A Github Actions workflow is set up to:
* Build the solution
* Verify code style
  - An `.editorconfig` file describes the code style which is enforced during development
  - The CLI `dotnet-format` [tool](https://github.com/dotnet/format) is used to enforce the code style during the CI workflow
* Perform SonarCloud analysis
  - This is the [SonarCloud project link](https://sonarcloud.io/dashboard?id=cvetomir-todorov_CecoChat)
* Build and push CecoChat containers to Docker Hub
  - [BFF](https://hub.docker.com/repository/docker/cvetomirtodorov/cecochat-bff)
  - [Messaging](https://hub.docker.com/repository/docker/cvetomirtodorov/cecochat-messaging)
  - [ID Gen](https://hub.docker.com/repository/docker/cvetomirtodorov/cecochat-idgen)
  - [User](https://hub.docker.com/repository/docker/cvetomirtodorov/cecochat-user)
  - [Chats](https://hub.docker.com/repository/docker/cvetomirtodorov/cecochat-chats)

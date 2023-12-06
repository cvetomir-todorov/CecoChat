# Internal design

Most services have the following layers and/or libraries associated with them:
* Service contracts
* Service data access
* Service host, endpoints, producers, consumers
* Service client

.NET projects are grouped into solution folders depending on the functionality:
* `Contracts` contains the contracts for the different parts of the system
* `Data` projects are tasked with data access for the different data stores
* `Server` contains the services which are ran from the IDE or are being containerized
* `Clients` projects are the .NET clients for calling a specific service
* `Shared` contains technology-oriented functionality, which could also be reusable in a different project
* `ClientApp` contains the console client used for testing and the load testing app

# Running

* Each service contains a customized `launchSettings.json` file which should be used in order to start it locally from the IDE
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

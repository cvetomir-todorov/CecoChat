# CI/CD

A Github Actions workflow is set up to:
* Build the solution
* Verify code style
  - An `.editorconfig` file describes the code style which is enforced during development
  - The CLI `dotnet-format` tool is run with `--check` and `--fix-style` to enforce the code style during the CI workflow
* Perform SonarCloud analysis
  - This is the [SonarCloud project link](https://sonarcloud.io/dashboard?id=cvetomir-todorov_CecoChat)
* Build and push CecoChat containers to Docker Hub
  - [Connect](https://hub.docker.com/repository/docker/cvetomirtodorov/cecochat-connect)
  - [History](https://hub.docker.com/repository/docker/cvetomirtodorov/cecochat-history)
  - [Materialize](https://hub.docker.com/repository/docker/cvetomirtodorov/cecochat-materialize)
  - [Messaging](https://hub.docker.com/repository/docker/cvetomirtodorov/cecochat-messaging)

# Run

Despite there is quite a bit of code written a good part of it is a proof-of-concept. In order to validate the implementation a lot of physical infrastructure is required which is quite expensive unfortunately. Nevertheless the system is functioning and with a machine powerful enough everything could be powered up **locally**. I've used `docker-compose` in order to run the required servers and the solution itself since it is also containerized. I've limited the memory for most of the containers to `512 MB`.

## Run 3rd party components

Before running the containers docker volumes need to be created using the [create volumes scripts](../run/create-volumes). Some containers have configuration which resides in the [configuration folder](../run/configuration). After running the containers they need to be [prepared](../run/prepare) using `docker exec -it` just one time (unless the volumes are deleted). The `docker-compose` files for the containers are in the [run folder](../run/).

* Kafka has 4 containers:
  - 2 Kafka brokers
  - Zookeeper
  - Kafdrop web interface
* Cassandra has 4 containers:
  - 3 Cassandra instances
  - Cassandra web interface
* Redis has 2 containers:
  - 1 Redis instance
  - Redis commander web interface

## Containerize and run CecoChat

In order to containerize CecoChat you can use the [containerize folder](../containerize/) which contains the Docker files for building the Docker images. Internally the Docker files do `dotnet publish` and use `Debug` configuration which has `Trace`/`Verbose` level of logging but this can be changed as prefered. The `docker-compose` file creates containers for:

* 1 connect server
* 2 messaging servers
* 1 materialize server
* 1 history server

## Clients

I've written a very basic console client. There is also a WPF desktop client for Windows which has some decent UI. A browser or WebAssembly client would be ideal in the future although that would require setting up a proxy to [enable gRPC-web](https://github.com/grpc/grpc-web).

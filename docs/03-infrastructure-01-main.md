# CI pipeline

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
  - [ID Gen](https://hub.docker.com/repository/docker/cvetomirtodorov/cecochat-idgen)
  - [Materialize](https://hub.docker.com/repository/docker/cvetomirtodorov/cecochat-materialize)
  - [Messaging](https://hub.docker.com/repository/docker/cvetomirtodorov/cecochat-messaging)

# Run locally

Despite there is quite a bit of code written a good part of it is a proof-of-concept. In order to validate the implementation a lot of physical infrastructure is required which is quite expensive unfortunately. Nevertheless the system is functioning and with a machine powerful enough everything could be powered up **locally**. I've used `docker-compose` in order to run the required servers and the solution itself since it is also containerized. I've limited the memory for most of the containers to `128 MB` or `256 MB` but there are a few which require `512 MB`.

## Run 3rd party components

Before running the containers some preparation steps need to be done manually. The scripts for them are in the respective technology folder. Docker volumes need to be created. After running the containers some need to be prepared using `docker exec -it` just one time (unless the volumes are deleted). The `docker-compose` files for the containers are in the [run folder](../run/).

### Container groups

* Cassandra
  - Containers
    - 2 Cassandra instances
    - Cassandra Web (management)
  - Preparation
    - Create Docker volumes
* Kafka
  - Containers
    - 2 Kafka brokers
    - ZooKeeper
    - Kafdrop (management)
  - Preparation
    - Create Docker volumes
    - Create Kafka topics
* Redis
  - Containers
    - 1 Redis instance
    - Redis commander (management)
  - Preparation
    - Create Docker volumes
    - Create initial configuration
* Observability
  - Containers
    - 1 Jaeger instance (all-in-one)
    - 1 ElasticSearch instance
    - 1 Fluentd instance
    - 1 Kibana instance (management)
  - Preparation
    - ElasticSearch - create Docker volumes
    - FluentD - build container

## Containerize and run CecoChat

In order to containerize CecoChat you can use the folder which contains the [Docker files](../run/cecochat/) for building the Docker images. Internally the Docker files do `dotnet publish` and use `Release` configuration but this can be changed as prefered. The `docker-compose` file creates containers for:

* 1 Connect server
* 2 Messaging servers
* 1 Materialize server
* 1 History server
* 1 ID Gen server

It uses `ASPNETCORE_ENVIRONMENT=Production` and overrides tracing options to persists all traces.

## Clients

I've written a very basic console client. There is also a WPF desktop client for Windows which has some decent UI. A browser or WebAssembly client would be ideal in the future although that would require setting up a proxy to [enable gRPC-web](https://github.com/grpc/grpc-web).

The machine running the clients needs to trust the [self-signed certificates](../source/certificates/).

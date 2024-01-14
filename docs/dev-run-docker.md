# Docker deployment

Make sure that the [prerequisites](dev-run-prerequisites.md) have been met before continuing

# Instances

* Integration
  - Kafka - 1 Zookeeper, 2 brokers
* Data storage
  - YugabyteDB - 1 master, 1 tserver
  - Cassandra - 2 nodes
  - Redis - 3 masters
  - MinIO - 1 node
* Observability
  - Telemetry - 1 OTel collector
  - Tracing - 1 Jaeger all-in-one
  - Metrics - 1 Prometheus, 1 cAdvisor, 1 Grafana
  - Logging - 1 ElasticSearch, 1 Kibana
* CecoChat
  - Config branch - 1 Config
  - BFF branch - 1 BFF, 1 User, 1 Chats
  - Messaging branch - 2 Messaging, 1 IDGen

# Manual setup

**Before** running most of the containers `docker volume` instances need to be created for the components below, using the `docker-volume.sh` scripts in their respective folder:

* Kafka
* Cassandra
* YugabyteDB
* Redis
* MinIO
* Logging

**After** the containers are ran the following steps need to be performed manually:

* Metrics
  - Grafana dashboards import

# Run

## Dynamic configuration

All services are using dynamic configuration, so a few components need to be always running:

* Components:
  - ConfigDB for storage
  - Config service for access and editing
  - Backplane for notifications
* Commands:
  - `docker compose -f kafka.yml up -d`
  - `docker compose -f yugabyte.yml up -d`
  - `docker compose -f cecochat-config.yml up -d`

## Application services

The convenience of `docker compose` makes it possible to run exactly what is needed:

* Working on the Messaging service:
  - Run the IDGen service from the IDE
  - `docker compose -f kafka.yml up -d`
* Working on Chats service:
  - `docker compose -f cassandra.yml up -d`
  - `docker compose -f kafka.yml up -d`
  - Run the IDGen and Messaging services from the IDE to input messages into Kafka
  - Run the BFF service from the IDE to make queries
* Working on User service:
  - `docker compose -f kafka.yml up -d`
  - `docker compose -f yugabyte.yml up -d`
  - `docker compose -f redis.yml up -d`
  - `docker compose -f minio.yml up -d`
  - Run the BFF service from the IDE to make queries
  - Run the Messaging services to receive connection notifications
* Working on observability:
  - `docker compose -f telemetry.yml up -d`
  - `docker compose -f tracing.yml up -d`
  - `docker compose -f metrics.yml up -d`
  - `docker compose -f logging.yml up -d`
* Start all CecoChat containers for the services in docker:
  - `docker compose -f cecochat-config.yml up -d`
  - `docker compose -f cecochat-messaging.yml up -d`
  - `docker compose -f cecochat-bff.yml up -d`
* In order to stop containers or destroy them:
  - `docker compose -f <some.yml> stop`
  - `docker compose -f <some.yml> down`

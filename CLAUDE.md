# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Project

CecoChat is a real-time chat engine built as a set of .NET microservices (system design in `docs/intro-design.md`, capabilities in `docs/intro-capabilities.md`).

# Build, lint, test

All commands run from the repo root unless noted.

* Certificates must be generated before building or testing (git-ignored, self-signed, everyone generates their own):
  ```
  cd source/certificates && bash create-certificate.sh && bash trust-certificate.sh
  ```
* Build: `dotnet build source/CecoChat.sln` (there is also `source/Check.sln`, a standalone solution for the hashing/connection-limit research checks under `Check.Connections.Client`, `Check.Connections.Server`, `Check.Hashing`)
* Lint / code style: `dotnet format source/CecoChat.sln --verify-no-changes --verbosity detailed` (style rules in `.editorconfig`, enforced in CI)
* Run all tests: `dotnet test source/CecoChat.sln`
* Run a single test project: `dotnet test source/<Project>.Testing/<Project>.Testing.csproj`
* Integration tests (`CecoChat.Chats.Testing`, `CecoChat.IdGen.Testing`) self-host the real service in-process against the generated certificate and hit it over the network — they are not pure unit tests. Each such test project needs a `CECOCHAT_START_TEST_CONTAINERS_<X>_DB=true` env var (e.g. `CECOCHAT_START_TEST_CONTAINERS_CHATS_DB`) to spin up its database via Testcontainers; without it, the test expects an already-running database (`ExistingChatsDb`-style fallback) at the address in `deploy/testing`.

# Architecture

## Services and their layering

Most functional areas follow the same project split, visible in the solution folder structure (`docs/intro-design.md`): `<Area>.Contracts` (messages/DTOs) → `<Area>.Data` (data access) → `<Area>.Service` (host, endpoints, producers/consumers) → `<Area>.Client` (typed client for other services to call it). Areas: Backplane, BFF, Chats, Config, IdGen, Messaging, User.

* **Config service** — every other service depends on it and must have it running first at startup. Dynamic configuration (see `docs/design-configuration.md`) lives in YugabyteDB; changes made through the Config service's Swagger endpoints publish notifications on a dedicated Kafka topic, and subscribed services re-pull the changed section via gRPC, then re-validate and hot-reload it. Static config still uses the normal `appsettings.json` / env-var override mechanism.
* **Messaging service** — the real-time send/receive path (`docs/design-messaging.md`). Each instance owns a fixed subset of Kafka partitions (`Hash(RecipientID) % PartitionCount`, FNV hash, verified in `Check.Hashing`); clients connect to whichever instance owns their partition, so the client-routing hash and the Kafka-partitioning hash must stay the same function. Kafka producers pick partitions manually (not Kafka's default auto-partitioning) because the service is stateful per-connection. Per-client message queues are bounded, so a slow client can have messages dropped — clients detect gaps via per-message counters and backfill from the Chats service (`docs/research-reliable-messaging-consistency.md`).
* **Chats service** — materializes the Kafka backplane stream into Cassandra: user chat state (newest message ID per chat) and full chat history, both the source of truth for their respective concerns (`docs/design-chats.md`). Chat state updates use a load-then-conditionally-write pattern instead of Cassandra lightweight transactions, deliberately, to avoid the latency/replica-contact cost of LWTs — safe here because each Kafka partition is consumed single-threaded and a user's messages always land in one partition.
* **User service** — registration/auth/profile/connections/files (`docs/design-users.md`). Passwords: PBKDF2-SHA512, 1,000,000 iterations, per-user salt, with a version token so the algorithm/iteration count can evolve later. User search uses PostgreSQL `pg_trgm` trigram indexes (min 3-char pattern, results capped and cached). Public profiles and connections are cached with TTL expiry; connection-change notifications don't invalidate the cache, an accepted tradeoff given low write frequency. User files are immutable, ≤512KB, stored in MinIO in per-day buckets (grouped by time, not by user, for predictable bucket sizes).
* **IdGen service** — generates Snowflake-style message IDs (timestamp-ordered), used by Chats to detect "newest" without a full ordering guarantee across Kafka partitions.
* **BFF service** — the HTTP entry point clients use to reach User/Chats/File storage; also needs partition/server-address config to route to the right Messaging instance.

## Solution folder grouping (source/CecoChat.sln)

* `Components` — one group per service area (Backplane, BFF, Chats, Config, IdGen, Messaging, User), each containing that area's Contracts/Data/Service/Client projects.
* `ClientApp` — `CecoChat.ConsoleClient` (manual testing) and `CecoChat.LoadTester`.
* `Shared` — project-specific code reusable across services (e.g. `CecoChat.Testing`).
* `Common` and `Common.<Tech>` (Cassandra, Kafka, Minio, Npgsql, OpenTelemetry, Redis, AspNet, Testing) — technology-oriented code with no CecoChat-specific knowledge, reusable in other projects.

## Cross-cutting

* Observability (`docs/design-observability.md`): OpenTelemetry throughout — health checks, distributed tracing (Jaeger), metrics (Prometheus/Grafana), log aggregation (ElasticSearch/Kibana).
* Local dev: run the .NET services from the IDE (each has a purpose-built `Properties/launchSettings.json`) against dependencies started via the docker-compose files in `deploy/docker`; `source/server-addresses.txt` is the canonical port map. Config service (+ its DB + Kafka backplane) must be up before any other service. Per-service dependency subsets for local dev are listed in `docs/dev-run-docker.md`.
* Deployment target beyond Docker Compose is Minikube/Kubernetes via Helm charts in `deploy/minikube` (`docs/design-deployment.md`).

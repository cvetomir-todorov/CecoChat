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
* Build: `dotnet restore source/CecoChat.sln` then `dotnet build --no-restore source/CecoChat.sln` (there is also `source/Check.sln`, a standalone solution for the hashing/connection-limit research checks under `Check.Connections.Client`, `Check.Connections.Server`, `Check.Hashing`)
* Lint / code style: `dotnet format source/CecoChat.sln --no-restore --verify-no-changes --verbosity detailed` (style rules in `.editorconfig`, enforced in CI)
* Prefer these exact command strings — `.claude/settings.json` allowlists the `--no-restore` forms, so variants trigger a permission prompt.
* Run all tests: `dotnet test source/CecoChat.sln`
* Run a single test project: `dotnet test source/<Project>.Testing/<Project>.Testing.csproj`
* Integration tests (`CecoChat.Chats.Testing`, `CecoChat.IdGen.Testing`) self-host the real service in-process against the generated certificate and hit it over the network — they are not pure unit tests. Each such test project needs a `CECOCHAT_START_TEST_CONTAINERS_<X>_DB=true` env var (e.g. `CECOCHAT_START_TEST_CONTAINERS_CHATS_DB`) to spin up its database via Testcontainers; without it, the test expects an already-running database (`ExistingChatsDb`-style fallback) at the address in `deploy/testing`.
* Package versions are managed centrally via `source/Directory.Packages.props` (NuGet Central Package Management, covers both `CecoChat.sln` and `Check.sln`). `.csproj` files use bare `<PackageReference Include="..." />` with no `Version` attribute — add or bump a version only in `Directory.Packages.props`.
* `dotnet build`/`restore`/`test`/`format` can hang (e.g. a stalled NuGet feed) — guard against this using the Bash tool's own `timeout` parameter, not by prefixing the command with the shell `timeout` command. Keeping the command string plain lets permission rules in `settings.json` match it directly.
* Target framework, C# language version, and nullable/implicit-usings are set once for every project in `source/Directory.Build.props` (currently .NET 10, C# 14) — bump them there, not per-`.csproj`.

# Architecture

## Services and their layering

Most functional areas follow the same project split, visible in the solution folder structure (`docs/intro-design.md`): `<Area>.Contracts` (messages/DTOs) → `<Area>.Data` (data access) → `<Area>.Service` (host, endpoints, producers/consumers) → `<Area>.Client` (typed client for other services to call it). Areas: Backplane, BFF, Chats, Config, IdGen, Messaging, User.

* **Config service** — every other service depends on it and must have it running first at startup. Dynamic configuration (see `docs/design-configuration.md`) lives in YugabyteDB; changes made through the Config service's Swagger endpoints publish notifications on a dedicated Kafka topic, and subscribed services re-pull the changed section via gRPC, then re-validate and hot-reload it. Static config still uses the normal `appsettings.json` / env-var override mechanism.
* **Messaging service** — the real-time send/receive path (`docs/design-messaging.md`). Each instance owns a fixed subset of Kafka partitions (`Hash(RecipientID) % PartitionCount`, FNV hash, verified in `Check.Hashing`); clients connect to whichever instance owns their partition, so the client-routing hash and the Kafka-partitioning hash must stay the same function. Kafka producers pick partitions manually (not Kafka's default auto-partitioning) because the service is stateful per-connection. Per-client message queues are bounded, so a slow client can have messages dropped — clients detect gaps via per-message counters and backfill from the Chats service (`docs/research-reliable-messaging-consistency.md`).
  - **Partition reassignment**: the Config service can live-reassign which server owns which partition sub-range without changing `PartitionCount` (the modulus stays fixed; only ownership moves). The physical Kafka partition count is a hardcoded ceiling set at topic-creation time (`CecoChat.Backplane/BackplaneInit.cs`, 12 in dev) — size it for the environment.
* **Chats service** — materializes the Kafka backplane stream into Cassandra: user chat state (newest message ID per chat) and full chat history, both the source of truth for their respective concerns (`docs/design-chats.md`). Chat state updates use a load-then-conditionally-write pattern instead of Cassandra lightweight transactions, deliberately, to avoid the latency/replica-contact cost of LWTs — safe here because each Kafka partition is consumed single-threaded and a user's messages always land in one partition.
* **User service** — registration/auth/profile/connections/files (`docs/design-users.md`). Passwords: PBKDF2-SHA512, 1,000,000 iterations, per-user salt, with a version token so the algorithm/iteration count can evolve later. User search uses PostgreSQL `pg_trgm` trigram indexes (min 3-char pattern, results capped and cached). Public profiles and connections are cached with TTL expiry; connection-change notifications don't invalidate the cache, an accepted tradeoff given low write frequency. User files are immutable, size-capped by `Files:MaxUploadedFileBytes` in `CecoChat.Bff.Service/appsettings.json` (10MB; note `docs/design-users.md` still says 512KB), stored in MinIO in per-day buckets (grouped by time, not by user, for predictable bucket sizes).
* **IdGen service** — generates Snowflake-style message IDs (timestamp-ordered), used by Chats to detect "newest" without a full ordering guarantee across Kafka partitions.
* **BFF service** — the HTTP entry point clients use to reach User/Chats/File storage; also needs partition/server-address config to route to the right Messaging instance.

## Solution folder grouping (source/CecoChat.sln)

* `Components` — one group per service area (Backplane, BFF, Chats, Config, IdGen, Messaging, User), each containing that area's applicable Contracts/Data/Service/Client projects (not every area has all four — e.g. `Backplane` is a single project with no client/data split).
* `ClientApp` — `CecoChat.ConsoleClient` (manual testing) and `CecoChat.LoadTester`.
* `Shared` — project-specific code reusable across services: `CecoChat.Server` (host bootstrap referenced by every `*.Service` project — entry point, exception handling, Serilog config, identity/auth registrations, observability wiring), `CecoChat.Data` (shared validation rules and data utilities), `CecoChat.Config` (legacy client-side dynamic-config abstraction — `IConfigChangeSubscriber`, `IRepo`, `ConfigSection`/`ConfigKeys`), and `CecoChat.Testing`.
* `Common` and `Common.<Tech>` (Cassandra, Kafka, Minio, Npgsql, OpenTelemetry, Redis, AspNet, Testing) — technology-oriented code with no CecoChat-specific knowledge, reusable in other projects.

## Conventions not visible from a single file

* **Contracts are protobuf, not C#.** `<Area>.Contracts` projects hold `.proto` files compiled by `Grpc.Tools` at build time; the C# types are generated, not checked in. A new `.proto` must be listed explicitly in the `.csproj` (`<Protobuf Include="X.proto" GrpcServices="All" />` for service definitions, without the attribute for plain messages) — dropping the file in the folder does nothing.
* **DB schema lives in scripts, not migrations.** `<Area>.Data/Scripts/*.sql` (Npgsql/Yugabyte) and `*.cql` (Cassandra) are run at startup by the `*DbInit` step and must be registered as `<EmbeddedResource>` in the `.csproj`; Npgsql scripts are ordered by filename (`table-01-…`, `table-02-…`). Both initializers are **create-only** — they no-op if the database/keyspace already exists, so a schema change means dropping the local DB, not writing a migration.
* **DI is Autofac.** Registrations are grouped into `*AutofacModule` classes next to the code they register, composed in each service's `Program.ConfigureContainer`. `builder.Services` is used only for framework-level registrations (auth, gRPC, health, OpenTelemetry).
* **Startup ordering is explicit.** Every `*.Service/Program.cs` has the same shape: `EntryPoint.CreateWebAppBuilder` → `AddServices`/`AddTelemetry`/`AddHealth` → `ConfigureContainer` → `ConfigurePipeline` → `EntryPoint.RunWebApp`. Ordered `InitStep` subclasses (`Common.AspNet/Init`) registered via `RegisterInitStep<T>` run sequentially before traffic is served; a failing step aborts startup. Each is paired with a startup-tagged health check, so adding an init step means adding its health check too.

## Cross-cutting

* Observability (`docs/design-observability.md`): OpenTelemetry throughout — health checks, distributed tracing (Jaeger), metrics (Prometheus/Grafana), log aggregation (ElasticSearch/Kibana).
* Local dev: run the .NET services from the IDE (each has a purpose-built `Properties/launchSettings.json`) against dependencies started via the docker-compose files in `deploy/docker`; `source/server-addresses.txt` is the canonical port map. Config service (+ its DB + Kafka backplane) must be up before any other service. Per-service dependency subsets for local dev are listed in `docs/dev-run-docker.md`.
* Deployment target beyond Docker Compose is Minikube/Kubernetes via Helm charts in `deploy/minikube` (`docs/design-deployment.md`). Service images are built from `package/cecochat/<service>.dockerfile` via `package/cecochat/build-all-images.sh`.
* CI (`.github/workflows/ci.yml`) runs lint → build (both solutions) → test with Coverlet coverage → SonarCloud. The default/integration branch is `dev`, not `main` — pull requests target `dev`.
* `docs/ai-detected-issues.md` is a register of defects the code currently has (distinct from `docs/what-next.md`, which is unstarted planned work). Check it before "fixing" something that looks broken — it may already be described there, with the reasoning.

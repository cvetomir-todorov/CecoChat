# What next

A short list of tasks to do next regarding product features, technical features, dev infrastructure, improvements etc.

# Essential features

* Users
  - Search users by name (pending full-text search support in YugabyteDB tracked in [this GitHub issue](https://github.com/yugabyte/yugabyte-db/issues/7850))
* Clients
  - Web-based
  - Mobile
* Development
  - Setup versioning

# Product features

* Users
  - Avatars
  - Change phone/email including confirmation
* Privacy
  - Add end-to-end encryption for messages
* Media messages
  - Image
  - Video
  - GIF
* Delivery notifications
  - Recipient has received the message
  - Recipient has seen the message
* Messaging
  - Deletion
  - Editing
* Group chats
  - Decide on the limitations
* Status updates
  - Online
  - Offline
  - Away
  - Busy

# Technical features

* Architecture
  - Design dynamic configuration
  - Design cross-region communication
* Security
  - Introduce an auth service
    - Issue access tokens, introduce refresh tokens, etc.
    - Restrict service-to-service communication
    - Use pepper instead of just salt for passwords
  - Rate limiting
  - Secure access to data storage components - Yugabyte, Cassandra, Redis
  - Secure access to integration components - Kafka
  - Secure access to observability components - logs database, Prometheus, Jaeger
* Observability
  - Metrics for system resources
  - Alerting
* Deployment
  - Infrastructure as code
  - Complete auto-scaling

# Development

* Automated testing
  - Setup infrastructure for test running
  - Add system tests
  - Add unit tests for each service
  - Add low-level tests for components worth the effort

# Improvements

* Performance
  - Use allocation-free logging
  - Make a larger benchmark and improve configuration of backplane, client-server communication, databases
* Observability
  - Add Open Telemetry metrics to Redis when StackExchange.Redis adds support
  - Add Open Telemetry metrics to UserDB when Npgsql adds support
  - Use Cassandra instrumentation from .NET contrib
  - Add metrics infrastructure to Minikube deployment
* Misc
  - Control sending notifications to SignalR connected clients as previously with gRPC clients
  - Improve monotonic clock skew and make snowflake ID generation to use it as `IdGen.ITimeSource`

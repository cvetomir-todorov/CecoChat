# What next

## Essential features

* Users
  - Friendship between users
* Clients
  - Web-based
  - Mobile
* Setup aggregated logs storage
* Setup versioning

## Product features

* Users
  - Avatars
  - Change phone/email including confirmation
  - User search
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

## Technical features

* Architecture
  - Design cross-region communication
* Security
  - Introduce an auth server
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
  - Auto-scaling
  - High availability (complete)

## Development

* Automated testing
  - Setup infrastructure for test running
  - Add system tests
  - Add unit tests for each service
  - Add low-level tests for components worth the effort

## Improvements

* Performance
  - Use allocation-free logging
  - Benchmark Kafka and improve its configuration
* Observability
  - Use Cassandra instrumentation from .NET contrib
  - Add Open Telemetry metrics in UserDB when Npgsql adds support
  - Improve existing distributed tracing - baggage propagation, custom instrumentation implementation
  - Improve existing log aggregation - performance of Fluentd, ElasticSearch index customizations
* Deployment
  - Create a redis cluster
* Misc
  - Control sending notifications to SignalR connected clients as previously with gRPC clients
  - Improve monotonic clock skew and make snowflake ID generation to use it as `IdGen.ITimeSource`
  - Improve error handling, resilience

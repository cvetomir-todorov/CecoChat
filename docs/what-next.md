# What next

A short list of tasks to do next regarding product features, technical features, dev infrastructure, improvements etc.

# Essential features

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
  - Use in-memory caching for user chat state consumer to avoid loading the chat state from the database each time
  - Make a larger benchmark and improve configuration of backplane, client-server communication, databases
* Files
  - Verify file signatures and scan file content
  - Store files in quarantine buckets initially
* Observability
  - Add Open Telemetry metrics to Redis when StackExchange.Redis adds support
  - Add Open Telemetry metrics to UserDB when Npgsql adds support
  - Use Cassandra instrumentation from .NET contrib
  - Add metrics infrastructure to Minikube deployment
* Misc
  - Clean up periodically the empty consumer groups for config-changes Kafka topic
  - Control sending notifications to SignalR connected clients as previously with gRPC clients
  - Improve monotonic clock skew and make snowflake ID generation to use it as `IdGen.ITimeSource`

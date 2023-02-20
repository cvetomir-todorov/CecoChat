# What next

## Essential features

* User/profile
  - User identity/register/login
  - Handle friendship between users
* Add clients
  - Web-based
  - Mobile
* Setup versioning

## Additional features

* User/profile
  - Edit user profiles
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
  - Rate limiting
  - Use secure transports and protocols on communication level
* Observability
  - Metrics for system resources
  - Alerting
* Deployment
  - Infrastructure as code
  - Load balancing
  - Auto-scaling
    - Distribute partitions between stateful service instances
  - Failover

## Development

* Automated testing
  - Setup infrastructure for test running
  - Add system tests
  - Add unit tests for each service
  - Add low-level tests for components worth the effort

## Improvements

* Performance
  - Benchmark Kafka and improve its configuration
* Observability
  - Add Open Telemetry metrics in UserDB when Npgsql adds support
  - Improve existing distributed tracing - baggage propagation, custom instrumentation implementation
  - Improve existing log aggregation - performance of Fluentd, ElasticSearch index customizations
* Misc
  - Improve monotonic clock skew and make snowflake ID generation to use it as `IdGen.ITimeSource`
  - Improve error handling, resilience, validation (e.g. gRPC requests)

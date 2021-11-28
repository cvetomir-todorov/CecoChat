# What next

## Essential features

* Add user/profile features
  - User authentication and authorization
  - Store user profile data
  - Handle friendship between users
* Add clients
  - Web-based
  - Mobile

## Additional features

* Privacy
  - Add end-to-end encryption for messages
* Media messages
  - Image
  - Video
  - GIF
* Delivery notifications
  - Recipient has received the message
  - Recipient has seen the message
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
  - Metrics for system resources, communication/db, app-specific
  - Monitoring
  - Health checks
* Deployment
  - Load balancing
  - Auto-scaling
  - Failover

## Development

* Infrastructure
  - Setup versioning
* Automated testing
  - Setup infrastructure for test running
  - Add system tests
  - Add unit tests for each service
  - Add low-level tests for components worth the effort

## Improvements

* Essential
  - Implement LRU cache in State service
  - Partition data for State service
* Performance
  - Benchmark Cassandra write batches and consider alternatives
  - Benchmark Kafka and improve its configuration
* Observability
  - Improve existing distributed tracing - baggage propagation, custom instrumentation implementation
  - Improve existing log aggregation - performance of Fluentd, ElasticSearch index customizations
* Misc
  - Improve design and add implementation so that clients are certain there are no missed messages
  - Improve monotonic clock skew and make snowflake ID generation to use it as `IdGen.ITimeSource`
  - Improve partition assignment when servers are added/removed in order to minimize disconnects
  - Improve error handling, resilience, validation (e.g. gRPC requests)

# What next

* The architecturally most important thing is to design the communication between cells which is also the most challenging
* Add missing operability elements
  - Observability
    - Metrics for system resources, communication/db, app-specific
    - Monitoring
    - Health check API
    - Exception tracking
  - Deployment
    - Failover
    - Elasticity based on load
* Security
  - Use secure transports and protocols on communication level
  - Add end-to-end encryption for messages
* Support different types of messages:
  - Group messages
    - Decide on the limitations
  - Media messages
    - Image
    - Video
    - GIF
  - Status update messages
    - Online
    - Offline
    - Away
    - Busy
  - Reaction emojis
  - Acks
    - Recipient has received the message
    - Recipient has seen the message
* Add user and profile related features
  - User authentication and authorization
  - Store user profile data
  - Handle friendship between users
* Add clients
  - Web-based
  - Mobile
  - Consider adding a separate API gateway for each type of client
* Setup automated testing
  - Setup infrastructure for test running
  - Add system tests
  - Add integration tests for each component
  - Add unit tests for parts worth the effort
* Infrastructure
  - Setup versioning
* Performance
  - Benchmark Cassandra write batches and consider alternatives
  - Benchmark Kafka and improve its configuration
* Improvements
  - Improve design and add implementation so that clients are certain there are no missed messages
  - Improve monotonic clock skew and make snowflake ID generation to use it as `IdGen.ITimeSource`
  - Improve partition assignment when servers are added/removed in order to minimize disconnects
  - Improve error handling, resilience, validation (e.g. gRPC requests)
  - Improve existing distributed tracing - baggage propagation, custom instrumentation implementation
  - Improve existing log aggregation - performance of Fluentd, ElasticSearch index customizations

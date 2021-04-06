# What next

* The architecturally most important thing is to design the communication between cells which is also the most challenging
* Add missing operability elements
  - Observability
    - Health check API
    - Log aggregation
    - Metrics and monitoring
    - Exception tracking
  - Deployment
    - Failover
    - Load
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
  - Acks
    - CecoChat has received the message
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
* Internal things and improvements
  - Improve design and add implementation so that clients are certain there are no missed messages
  - Improve error handling and add resilience
  - Compare Materialize servers immediate message write to bulk-loading data
  - Benchmark Kafka and improve its configuration
  - Improve existing distributed tracing - baggage propagation, custom instrumentation implementation

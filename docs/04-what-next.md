# What next

* The architecturally most important thing is to design the communication between cells which is also the most challenging
* Add missing operability elements
  - Observability
    - Health check API
    - Log aggregation
    - Distributed tracing
    - Metrics and monitoring
    - Exception tracking
  - Deployment
    - Failover
    - Load
* Internal things could be worked on
  - Benchmark Kafka and configure it accordingly
  - Compare Materialize servers immediate message write to bulk-loading data
* Add different types of messages:
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

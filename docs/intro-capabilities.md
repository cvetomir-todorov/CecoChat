# Capabilities

## Functional

* Messaging in real-time
  - Send and receive messages, (un)react with emojis
  - Notifications when a message has been processed
  - Multiple clients for the same user
* Chats
  - Indication for new messages
  - Review history at a random point in time
* User profiles with full and public-only data

#### Limitations

* No user register/login/identity
* No user friendship
* No web/mobile clients (only a client for development purposes)

## Non-functional

* Reliability
  - Durability is guaranteed by acknowledgement and replication
  - Eventual consistency guarantees - once the sent message is processed it will eventually be persisted and delivered
* Scalability
  - Designed for up to 10 mln of simultaneously active users
  - Unfortunately, expensive to validate due to the infrastructure required
  - Linear scalability of the main technologies (Kafka, Cassandra, YugabyteDB)
  - Supported by numbers from the [calculations](research-calculations.md)
  - Minimal [load test on 2 machines](load-test.md)
* Security
  - TLS for Kubernetes cluster and services
  - Access tokens for authn and authz
* Observability
  - Health
  - Distributed tracing
  - Log aggregation
  - Metrics
  - Monitoring
* Deployment
  - Containerization
  - Load balancing
  - High availability (partial)
* Maintainability
  - Simple design
  - Code quality gate
  - Minimal documentation
  - Open-source technologies

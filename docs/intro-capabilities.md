# Capabilities

## Functional

* Messaging in real-time
  - Send and receive messages, react and unreact with emojis
  - Notifications when a message has been processed
  - Multiple clients for the same user
* Chats
  - Shown with indication for new messages
  - Review history at a random point in time
* User profiles with full and public-only data

## Limitations

* No user register/login/identity
* No user friendship
* No web/mobile clients (only a client for development purposes)

## Non-functional

* Reliability
  - Durability is preferred at the cost of some latency
  - Eventual consistency guarantees - once the sent message is processed it will eventually be persisted and delivered
* Scalability
  - Designed for up to 10 mln of simultaneously active users, which unfortunately is expensive to validate due to the infrastructure required
  - Supported by numbers from the [calculations](research-calculations.md)
  - Linear scalability of the main technologies (Kafka, Cassandra, YugabyteDB) 
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
  - Minimal documentation
  - Code quality gate
  - Open-source technologies

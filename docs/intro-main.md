# Capabilities

## Functional features

* User can send messages to other users
* User receives messages from other users in real-time
* User can (un)react to messages with emojis
* User can connect with multiple clients
* User is shown chats with indication of new messages
* User can review chat history at a random point in time 
* User receives a delivery notification when the message has been processed

## Limitations
* No user profile and friendship are implemented
* No clients exist, only a client for development purposes

## Non-functional features

* Reliability
  - Durability is preferred at the cost of some latency
  - Eventual consistency guarantees - once the sent message is processed it will eventually be persisted and delivered
* Scalability
  - Designed for 10-100 mln of active users, which unfortunately is expensive to validate due to the infrastructure required
  - Supported by numbers from the [back-of-the-envelope calculation](design-back-of-the-envelope.md)
  - Linear scalability of the main technologies (Kafka, Cassandra) 
* Security
  - TLS used on communication level
  - Access tokens for authn and authz
* Observability
  - Distributed tracing
  - Log aggregation
  - Metrics
  - Monitoring
* Deployment
  - Containerization
* Maintainability
  - Code quality gate
  - Open-source technologies

# Capabilities

## Functional

* User can send messages to other users
* User receives messages from other users in real-time
* User can (un)react to messages with emojis
* User can connect with multiple clients
* User is shown chats with indication of new messages
* User can review chat history at a random point in time 
* User receives a delivery notification when the message has been processed
* User has a profile with full and public-only data
* User can see public profiles of other users

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
  - Linear scalability of the main technologies (Kafka, Cassandra) 
* Security
  - TLS used on communication level
  - Access tokens for authn and authz
* Observability
  - Health
  - Distributed tracing
  - Log aggregation
  - Metrics
  - Monitoring
* Deployment
  - Containerization
* Maintainability
  - Code quality gate
  - Open-source technologies

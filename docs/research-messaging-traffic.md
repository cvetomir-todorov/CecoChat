# Exchange messages between different messaging servers

When two users are connected to different messaging servers the latter need to communicate somehow.

# Approach #1 - direct communication

Messaging servers exchange messages directly between each other.

Pros
* Lower latency
* Messaging servers are stateless
* Easy addition of new messaging servers
* Standard load-balancing when a user connects initially to a single messaging server

Cons
* Each messaging server needs to know which other messaging server the recipient of the message is connected to:
    - One option is each messaging server to keep an in-memory data structure for the up to 10 mln clients. It's a challenge to implement one considering thread-safety. Additionally it is expensive in terms of memory.
    - Another option is to offload the storage of this knowledge to a data store. This would increase the latency a bit and add an additional element in the architecture.
* When a user connects or disconnects the storage which maps the user with its messaging server needs to be updated.
* Messaging servers need to keep open connections between each other. This does not play well with the concurrent connection limits.
* Messaging servers need to know when one of them fails, find about new nodes and establish connections to them.
* Consistency of the data would be more challenging since the two main operations required for message processing would be separate:
    - Sending the message to its recipient(s) by calling one (or multiple for group chats) messaging server(s)
    - Persisting the message into the chats database

# Approach #2 - indirect communication

Messaging servers exchange messages via a PUB/SUB backplane via a message broker.

Pros
* Messaging servers need to know only the topic/queue/channel/etc. for the recipient. They still need to keep connections to the PUB/SUB backplane but that number is smaller.
* Messaging servers do not need to know about and keep connection between each other.
* Consistency problem could be solved since message processing when a message is received is a single action - placing the message in the PUB/SUB backplane. There would be 2 subscribers - one being the messaging server for the recipient and the other - the component for persisting the message into the chats database. Of course at the very best what we would have is eventual consistency.
* The PUB/SUB backplane would also act like an event log.

Cons
* Higher latency because of the indirect communication, especially if we persist the message in the PUB/SUB backplane.
* There needs to be balanced distribution of messaging traffic between all messaging servers. We cannot afford **every** messaging server to process **all** messages. Each messaging server would need to receive only a part of the messages which would be dedicated only to specific users. Which would mean those specific users need to connect to a specific messaging server. As a result client load-balancing during initial connect becomes a challenge.
* Each messaging server needs to listen to a specific part of the messages. The addition of new messaging servers would add complexity to the deployment infrastructure. A centralized configuration could be used to push changes to working messaging servers.
* Each messaging server is the only one responsible for specific clients. The deployment infrastructure can keep idle messaging servers ready to replace ones declared dead.
* As a result the messaging servers become stateful and unique, but at least still easily replaced.

# Decision

The approach using a PUB/SUB backplane was chosen because of the pros listed at the cost of complexity increase in deployment and configuration. Kafka is particularly suitable to this approach, although partition assignment for the messaging server's Kafka consumer group need to be manual. There needs to be centralized dynamic configuration with mapping between messaging server and partitions assigned to it. Additionally the addresses of all messaging servers would need to be present so clients know where to connect to.

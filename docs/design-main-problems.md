# Main problems

* Back of the envelope calculations
* Exchange messages between different messaging servers
* Message IDs
* Reliable messaging and consistency

# Back of the envelope calculations

The [back-of-the-envelope](design-back-of-the-envelope.md) file contains the detailed calculations. They are based on the [concurrent connections limit](design-connection-limit.md) showing that a messaging server is limited to **64 k connections**. The calculation tells us that **1.6 k messaging servers** are needed in order to support **100 mln active users**. We would consider **256 bytes message size**.

## Daily 24 hour usage

Calculating the daily usage with **640 mln users** spread throughout the day each of which sends **128 messages per day** gives us **950 000 messages/s for the cell** and **232 MB/s for the cell** with **0.15 MB/s per messaging server**.

## Peak 1 hour usage

Calculating a peak usage for **1 hour** daily where **80%** of the maximum users - **80 mln active users** send **50%** of their daily messages - **64 messages** we get **1 423 000 messages/s for the cell** and **348 MB/s for the cell** with **0.22 MB/s per messaging server**.

## Conclusion

These numbers do not take into account the security and transport data overhead. Additionally, this traffic would be multiplied. For example sending a message would require that data to be passed between different layers, possibly to multiple recipients. Numbers are not small when we look at the system as a whole. But for a single messaging server the throughput is tiny. The system is limited by how many concurrent connections a messaging server can handle. That means we would need a lot of messaging servers and linearly scalable technologies and we need to support a high level of concurrency. The usage of Kafka, Cassandra and the .NET async programming model make a good start.

# Exchange messages between different messaging servers

When two users are connected to different messaging servers the latter need to communicate somehow.

## Approach #1 - direct communication

Messaging servers exchange messages directly between each other.

Pros
* Lower latency
* Messaging servers are stateless
* Easy addition of new messaging servers
* Standard load-balancing when a user connects initially to a single messaging server

Cons
* Each messaging server needs to know which other messaging server the recepient of the message is connected to:
  - One option is each messaging server to keep an in-memory data structure for the up to 100 mln clients. It's a challenge to implement one considering thread-safety. Additionally it is expensive in terms of memory.
  - Another option is to offload the storage of this knowledge to a data store. This would increase the latency a bit and add an additional element in the architecture.
* When a user connects or disconnects the storage which maps the user with its messaging server needs to be updated.
* Messaging servers need to keep open connections between each other. This does not play well with the concurrent connection limits.
* Messaging servers need to know when one of them fails, find about new nodes and establish connections to them.
* Consistency of the data would be more challenging since the two main operations required for message processing would be separate:
  - Sending the message to its recipient(s) by calling one (or multiple for group chats) messaging server(s)
  - Persisting the message into the history database

## Aproach #2 - indirect communication

Messaging servers exchange messages via a PUB/SUB backplane via a message broker.

Pros
* Messaging servers need to know only the topic/queue/channel/etc. for the recipient. They still need to keep connections to the PUB/SUB backplane but that number is smaller.
* Messaging servers do not need to know about and keep connection between each other.
* Consistency problem could be solved since message processing when a message is received is a single action - placing the message in the PUB/SUB backplane. There would be 2 subscribers - one being the messaging server for the recipient and the other - the component for persisting the message into the history database. Of course at the very best what we would have is eventual consistency.
* The PUB/SUB backplane would also act like an event log.

Cons
* Higher latency because of the indirect communication, especially if we persist the message in the PUB/SUB backplane.
* There needs to be balanced distribution of messaging traffic between all messaging servers. We cannot afford **every** messaging server to process **all** messages. Each messaging server would need to receive only a part of the messages which would be dedicated only to specific users. Which would mean those specific users need to connect to a specific messaging server. As a result client load-balancing during initial connect becomes a challenge.
* Each messaging server needs to listen to a specific part of the messages. The addition of new messaging servers would add complexity to the deployment infrastructure. A centralized configuration could be used to push changes to working messaging servers.
* Each messaging server is the only one responsible for specific clients. The deployment infrastructure can keep idle messaging servers ready to replace ones declared dead.
* As a result the messaging servers become stateful and unique, but at least still easily replaced.

## Decision

The approach using a PUB/SUB backplane was chosen because of the pros listed at the cost of complexity increase in deployment and configuration. Kafka is particularly suitable to this approach, although partition assignment for the messaging server's Kafka consumer group need to be manual. There needs to be centralized dynamic configuration with mapping between messaging server and partitions assigned to it. Additionally the addresses of all messaging servers would need to be present so clients know where to connect to.

# Message IDs

Each message needs to have unique ID. Additionally it is associated with a timestamp. There are a few ways to do it.

## UUID/GUID + timestamp

UUID/GUID instances are globally unique and easily generated. Timestamps are easily generated as well. Unfortunately these two have larger size of 16 bytes for UUID/GUID and 8 bytes for timestamps. That means 24 bytes in total.

Cassandra is a KKV database. The V stands for value and the KK means that there are 2 keys. The first K is a partitioning key and indicates which partition the data is in. We would use the user ID or chat ID as Cassandra partitioning key. The second K is a clustering key and is used to physically sort the data in that partition. Since we want to make range queries by time it makes sense to use the timestamp as Cassandra clustering key.

With these indexes in place if we want to make a query by message ID we won't be able to do so in Cassandra as there's no such an index. We can only make such a query by the clustering key which is a timestamp. Using timestamps instead of message IDs is dangerous due to their possible duplication and different representation in different systems. Ideally we would want to use message IDs which are guaranteed to be unique and are simpler. Unfortunately UUID/GUIDs are not simple. And even if they were we would need a secondary index for that kind of query. Cassandra secondary indexes are dangerous as they are spread throughout the nodes in the cluster. That means a query using a secondary index could affect much more nodes that we want to, reducing performance in result.

## Snowflake

Twitter snowflakes are 8-byte or int64 IDs which higher bits contain timestamp value. That means they are time sortable. Using snowflakes as time-sortable message IDs allow us to use them in Cassandra as a clustering key. That way we can make ranged time-based queries and also make queries by message ID. Since snowflakes are int64 their representation is determinate in different systems. Apart from the sign bit maybe, which is why some libs do not use it.

In order to fit more timestamps snowflakes use an epoch - `timestamp = epoch-ticks + snowflake-ticks`. There are 2 problems though in a system that is highly scalable and distributed.
* We need to generate more than 1 ID during a snowflake tick. In order to solve this problem part of the bits in the snowflake are used as a **sequence**. This means that for a given snowflake tick we have a number of IDs and only when they end we would need to wait for a new tick to occur.
* We also need to generate IDs in a distributed way instead of a centralized way. To solve this problem part of the bits in the snowflake are used as a **generator ID**. This way we can use independent servers, processes, threads to generate IDs.

The way snowflakes are used is - `41 timestamp bits + 8 generator bits + 14 sequence bits`. That means we have up to **256 generators**, each of which can generate up to **16384 IDs** per snowflake tick. We're using **1 ms tick** which means that we have close to **70 years interval**.

If we generated message IDs in messaging service we would need a lot of generator IDs since we have a lot of messaging servers. In order to assign less bits for the generator ID we introduce a dedicated ID Gen service. This introduces an additional element in the system and increases the operation complexity. The latency is also increased since messaging service now need to obtain an ID from the ID Gen service. When the IDs are requested in a batch though that effect is reduced at the cost of some additional complexity in the implementation.

# Reliable messaging and consistency

## Sending messages

Kafka provides a delivery handler callback which is used to send an ACK to clients indicating the persistance status. In order to guarantee durability the replication factor needs to be increased to 2 or 3. There needs to be at least 1 in-sync replica. Using this configuration once the message gets into the Kafka cluster then the small likelyhood of 2 brokers failing will mean a very small percentage of messages will be lost. If the ACK is negative then automatic retry would not be used in order to avoid self-inflicted DDoS. The client UI would simply present the user with indication that the message hasn't been processed and interactive means for it to be re-sent.

## Receiving messages

After each client connects or reconnects it gets all missed messages from the history service. In the messaging service the client is dedicated an in-memory message queue which is bounded. When the message queue is full that would mean newer messages will be dropped. In order for the client to know about that - each new message triggers an increase to a session counter sent along with the message itself. If a client sees a gap it may use the history service in order to obtain missing messages.

## Consistency

In order for clients to know about missing messages they can check for new messages at reasonable intervals. That would be costly in terms of traffic so the session-based counters could be used as a primary indicator. That may increase the interval but not obsolete the regular checks.

To reduce the need for regular checks - these could be performed only for the most recent messages, not all messages exchanged in a chat. If the clients requires previous chat history they can use the history service after explicit user UI interaction.

To make checking for missing messages cheaper we can use Cassandra custom aggregate functions. A custom one could calculate a hash for messages in a given interval. It can use the message ID and would be very effective since data won't leave the database. Clients can utilize it by getting a hash of the messages in a given interval, calculate the same for the messages they have locally for the same interval and compare them. Only if the hashes differ clients would request the messages in that interval.

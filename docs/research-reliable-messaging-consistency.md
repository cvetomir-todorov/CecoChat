# Reliable messaging and consistency

Achieving reliable messaging and consistency requires a number of design decisions. 

# Sending messages

Kafka provides a delivery handler callback which is used to send an ACK to clients indicating the persistence status. In order to guarantee durability the replication factor needs to be increased to 2 or 3. There needs to be at least 1 in-sync replica. Using this configuration, once the message gets into the Kafka cluster, with the small likelihood of 2 brokers failing, there would be a very small percentage of messages lost. If the ACK is negative then automatic retry would not be used in order to avoid self-inflicted DDoS. The client UI would simply present the user with indication that the message hasn't been processed and interactive means for it to be re-sent.

# Receiving messages

After each client connects or reconnects it gets all missed messages from the chats service. In the messaging service the client is dedicated an in-memory message queue which is bounded for performance reasons. When the message queue is full that would mean newer messages will be dropped. In order for the client to know about that - each new message triggers an increase to a session counter sent along with the message itself. If a client sees a gap it may use the chats service in order to obtain missing messages.

# Consistency

In order for clients to know about missing messages they can check for new messages at reasonable intervals. That would be costly in terms of traffic so the session-based counters could be used as a primary indicator. That may increase the interval but not obsolete the regular checks.

To reduce the need for regular checks - these could be performed only for the most recent messages, not all messages exchanged in a chat. If the clients requires previous chat history they can use the chats service after explicit user UI interaction.

To make checking for missing messages cheaper we can use Cassandra custom aggregate functions. A custom one could calculate a hash for messages in a given interval. It can use the message ID and would be very effective since data won't leave the database. Clients can utilize it by getting a hash of the messages in a given interval, calculate the same for the messages they have locally for the same interval and compare them. Only if the hashes differ clients would request the messages in that interval.

# Send and receive approach

We have a limit of how many clients can connect to a messaging server. A key design element is how to distribute the messages between the messaging servers. Which is what two clients connected to different messaging servers would need in order to exchange messages.

## Standard approach

The standard approach which I observed a few times when I researched this problem was for the messaging servers to communicate directly.

The benefits of this approach are:

* Lower latency because of the direct communication.
* Standard load-balancing when a user connects initially to a single messaging server.
* Messaging servers are stateless.

Drawbacks are not small:

* Each messaging server needs to know which other messaging server the recepient of the message is connected to
  - One option is each messaging server to keep an in-memory data structure for the up to 100 mln clients which isn't something easy to implement, especially if we take into account the fact that this data structure needs to be thread-safe. A hashtable, b-trees or some variant of trie are possible options.
  - Another option is to offload the storage of this knowledge to a configuration database cluster. This would increase the latency of course.
* Messaging servers need to keep open connections between each other. This does not play well with one of the key limits in the system which is the number of connections to a messaging server.
* Messaging servers need to know when one of them fails and re-establish the connection to its replacement.
* Consistency of the data would be harder since the two logical operations required for message processing would be separate instead of a single one.
  - Sending the message to its recipient(s) by calling one (or multiple for group chats) messaging servers
  - Persisting the message into the history database

## Alternative approach

I decided to explore a different approach for dealing with the drawbacks from the standard one. It is to rely on a PUB/SUB backplane resulting in indirect communication between messaging servers. The PUB/SUB backplane also acts like an event log.

The benefits are:

* Messaging server needs to know only the topic (or in the case of Kafka - the topic partition) for the recipient, which is something easily calculated locally. It still needs to keep connections to the Kafka cluster nodes but they are smaller in number.
* Messaging servers do not need to know about and keep connection between each other.
* Consistency problem is partially solved since message processing when a message is received is a single action - placing the message in the PUB/SUB backplane. Of course this implies using a PUB/SUB technology like Kafka which allows different consumer groups which process messages independently from each other. And at the very best what we have is eventual consistency.

The drawbacks, just like the benefits, are the opposite from the previous approach

* Higher latency because of the indirect communication, especially if we persist the message in Kafka to not just the leader but to at least 1 more in-sync replica.
* Client load-balancing becomes non-trivial, since balanced distribution of topic partitions between all messaging servers now is crucial. Manually assigning topic partitions in Kafka is considered a custom approach, compared to the built-in auto-balancing.
* The messaging servers become stateful since they are now bound to 2 things.
  - Each messaging server needs to listen to a specific set of topic partitions. This can be solved via centraized configuration.
  - Each messaging server is the only one responsible to the set of clients which use the mentioned topic partitions. To solve this issue the deployment infrastructure can keep idle messaging servers ready to replace ones declared dead.

## Decision

While the standard approach looks a more solid solution especially latency-wise I decided to try out the alternative one using a PUB/SUB backplane.

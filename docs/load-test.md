# Load testing scenario

The aim is to bring online as many clients as possible and each of them to behave like a real user. In the init phase each user is assigned friends to chat with.

The behavior of each user is:
* Log in
* Perform **3** sessions
  - Pause for random seconds within [1, 9]
  - Load user chats
    - Get user chats
    - Get user profiles for the chats
  - Pause for random seconds within [2, 4]
  - Load a chat with one of the friends
    - Get chat history
    - Get user profile
  - Pause for random seconds within [3, 5]
  - Send **4** messages
    - Pause for random seconds within [3, 5] between messages

Pauses are added to simulate users reading/typing/making decisions. Their random values make the load a bit more spread so sharp spikes of resource utilization are avoided.

# Hardware

The test hardware is rather limited

| Machine        | CPU            | Frequency | Cores | Threads | RAM  | Storage | OS           |
|:---------------|:---------------|:----------|:------|:--------|:-----|:--------|:-------------|
| Old desktop    | Core i5-3450   | 3.50 GHz  | 4     | 4       | 32GB | SSD     | Ubuntu 22.04 |
| Dell Precision | Core i7-12800H | 4.80 GHz  | 14    | 20      | 32GB | SSD     | Ubuntu 22.04 |

# Components setup

`Old desktop` hosted:
* Backplane (Kafka)
* ChatsDB (Cassandra)
* ConfigDB (Redis)

`Dell Precision` hosted:
* UserDB (YugabyteDB)
* Application servers:
  - 1 ID Gen
  - 2 Messaging
  - 1 Chats
  - 1 User
  - 1 BFF
* Load tester representing the clients

A customized Docker deployment was used. The application servers were built in Release and their config was adjusted.

# Results

* Initially `Old desktop` hosted the UserDB too, but since it ran out of CPU, UserDB was moved to `Dell Precision` which had more available
* `Old desktop` still ran out of CPU first but at least the `Dell Precision` was also close to its max CPU
* The load on the SSDs was low and memory was more than enough
* When the CPU maxed out the application servers started reporting timeout errors, which crashed the affected clients
* 6000 clients were able to successfully connect and complete the scenario

# Conclusions

* 6000 clients doesn't seem a lot, but that number is due to CPU exhaustion:
  - TLS takes its toll
  - Storage and messaging infrastructure is quite CPU intensive under stress
  - Application servers didn't use a lot of CPU compared to the storage and messaging infrastructure
  - Reminder that the bottleneck during the [connection limit research](research-connection-limit.md) where clients were sending a message each second (and where we didn't have TLS and storage and messaging infrastructure) was port exhaustion 
* It was easy to see by the CPU load whether clients are sending messages (and putting stress on Backplane, ChatsDB) or are requesting user profiles (and putting stress on UserDB)
* Adding caching to the User service should probably reduce the load on UserDB
* For a realistic load test much more compute power is needed
* Still there were no obvious downsides to the scalability and the initial claim during the design phase that the solution should be horizontally scalable still has no reason not to hold

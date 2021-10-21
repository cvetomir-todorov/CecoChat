# Capabilities

## Functional features

* User can send messages to other users
* User receives messages from other users in real-time
* User can (un)react to messages with emojis
* User can connect with multiple clients
* User is shown the missed messages
* User can review chat history at a random point in time between him and another user 
* User receives a delivery notification when the message has been processed

Limitations:
* No user profile and friendship are implemented so clients rely on user IDs.
* No clients exist, only ones for development purposes.

## Non-functional features

* Reliability
  - Durability is preferred at the cost of some latency
  - Eventual consistency guarantees - once the sent message is processed it will eventually be persisted and delivered
* Scalability
  - Designed for 10-100 mln of active users, which unfortunately is expensive to validate due to the infrastructure required
  - Numbers from the [back-of-the-envelope calculation](01-intro-02-back-of-the-envelope.md) and the linear scalability of the main technologies show that the solution is realistic
* Security
  - TLS used on communication level
  - Access tokens for authn and authz
* Maintainability
  - Operations
    - Distributed tracing
    - Log aggregation
  - Development
    - Sonarcloud quality gate
    - Open-source technologies

## More

[Design approach](02-design-01-approach.md) and the whole design section go into more detail for the different areas of the solution. [What next](05-what-next.md) covers capabilities to be added.

# Concurrent connections benchmark

This is a benchmark for the number of concurrent connections per messaging server. The code is in the [check](../check/) folder.

## Hardware setup

I used two machines connected via 100Mbps router.

| Machine     | CPU         | Frequency | Cores | RAM  | OS                      |
| :---------- | :---------  | :-------- | :---- | :--- | :---------------------- |
| Weaker      | Core 2 Duo  | 2133MHz   | 2     | 4GB  | Ubuntu Server 20.04 LTS |
| Moderate    | QuadCore i5 | 3533MHz   | 4     | 16GB | Windows 10 20H2         |

## Scenario and results

On the server I am using gRPC services hosted by ASP.NET Core which utilize .NET async programming model. The clients connect first and then simultaneously start sending 20 messages at a rate of 1 per second. Below are the results specifying which machine ran the client and what are the server process resources allocated.

| Client machine | Clients succeeded | Client time | Server machine | Server CPU | Server threads | Server RAM |
| :------------- | :---------------- | :---------- | :------------- | :--------- | :------------- | :--------- |
| Moderate       | 15869             | 21 seconds  | Weaker         | 150%-200%  | ?              | 1.3GB      |
| Weaker         | 28232             | 145 seconds | Moderate       | 40-50%     | 67             | 3.55GB     |

## Analysis

The `Clients succeeded` is the number of clients that were able to connect. That number is a result of port exhaustion limits hit on both OS-es which **ran the clients**, not the servers. The Windows error was `An operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full`. The Ubuntu Server one was `Cannot assign requested address`.

Strangely when clients were on the weaker machine the client time required in order to complete the requests is considerably higher while it was obvious that the server could handle more load. One of these could be the issue: the client code could be improved to manually schedule the tasks instead of relying on the .NET async programming model, the weaker machine is really weak, Windows 10 has some limits which prohibit it from handling a high number of concurrent clients, there is a concurrency issue with .NET 5 on Ubuntu Server.

## Conclusion

The client-side port exhaustion limitation will be mitigated by the fact that each client is typically on a separate device. Therefore the server-side limitations remaining are the resources allocated for each connected client. Based on these numbers **64 k concurrent connections** per messaging server could be a realistic number and would be a useful limit in the calculations. It is a round number close to the **65535 max number of ports** for the current OS-es.

# Back of the envelope calculations

The [back-of-the-envelope](01-intro-02-back-of-the-envelope.md) file contains the detailed calculations. A messaging server is the server to which users directly connect to. A key limit is **64 k connections per messaging server**. A simple calculation tells that **1.6 k messaging servers** are needed in order to support **100 mln active users**. We would consider **256 bytes message size**.

## Daily 24 hour usage

Calculating the daily usage with **640 mln users** spread throughout the day each of which sends **128 messages per day** gives us **950 000 messages/s for the cell** and **232 MB/s for the cell** with **0.15 MB/s per messaging server**.

## Peak 1 hour usage

Calculating a peak usage for **1 hour** daily where **80%** of the maximum users - **80 mln active users** send **50%** of their daily messages - **64 messages** we get **1 423 000 messages/s for the cell** and **348 MB/s for the cell** with **0.22 MB/s per messaging server**.

## Conclusion

These numbers do not take into account the security and transport data overhead. Additionally, this traffic would be multiplied. For example sending a message would require that data to be passed between different layers, possibly to multiple recipients and stored multiple times in order to enable faster querying.

Numbers are not small when we look at the cell as a whole. But for a single messaging server the throughput is tiny. Using linearly scalable technologies would allow us to achieve the desired throughput.

# Why

I decided to take on the challenge to design a globally-scalable chat like WhatsApp and Facebook Messenger. Based on [statistics](https://www.statista.com/statistics/258749/most-popular-global-mobile-messenger-apps/) the montly active users are 2.0 bln for WhatsApp and 1.3 bln for Facebook Messenger. At that scale I decided to start a bit smaller. A good first step was to design a system that would be able to handle a smaller number of active users which are directly connected to it. Let's call it a cell. After that I would need to design how multiple cells placed in different geographic locations would communicate with each other. I certainly don't have the infrastructure to validate the design and the implementation. But I used the challenge to think at a large scale and to learn a few new technologies and approaches along the way.

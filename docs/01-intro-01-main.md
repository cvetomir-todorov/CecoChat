# Why

I decided to take on the challenge to design a globally-scalable chat like WhatsApp and Facebook Messenger. Based on [statistics](https://www.statista.com/statistics/258749/most-popular-global-mobile-messenger-apps/) the montly active users are 2.0 bln for WhatsApp and 1.3 bln for Facebook Messenger. At that scale I decided to start a bit smaller. A good first step was to design a system that would be able to handle a smaller number of active users which are directly connected to it. Let's call it a cell. After that I would need to design how multiple cells placed in different geographic locations would communicate with each other. I certainly don't have the infrastructure to validate the design and the implementation. But I used the challenge to think at a large scale and to learn a few new technologies and approaches along the way.

# Capabilities

## Functional

* User can send messages to and receive messages from other users
* User is shown at log-in the missed messages while being offline
* User can review chat history between him and another user
* User receives an ACK when the message has been processed by the chat, but not yet delivered to recipient
* Same user can use multiple clients

Currently no user profile and friendship are implemented so clients rely on user IDs.

## Non-functional

* Designed for 10-100 mln of active users
  - Expensive to validate for real
  - Numbers from the calculation show that the solution is possible
  - The concurrent connection benchmark is promising
* A balanced aproach between
  - Latency
  - Consistency
  - Fault tolerance
* TODO: list the actual non-functional capabilities

# Concurrent connections benchmark

This is a benchmark for the number of concurrent connections per messaging server. The code is in the [check](../check/) folder. I used two machines connected via 100Mbps router. Details are as follow:

| Machine     | CPU         | Frequency | Cores | RAM  | OS                      |
| :---------- | :---------  | :-------- | :---- | :--- | :---------------------- |
| Weaker      | Core 2 Duo  | 2133MHz   | 2     | 4GB  | Ubuntu Server 20.04 LTS |
| Moderate    | QuadCore i5 | 3533MHz   | 4     | 16GB | Windows 10 20H2         |

On the server I am using ASP.NET Core gRPC services utilizing async-await and TPL. The clients connect first and then simultaneously start sending 20 messages at a rate of 1 per second again utilizing async-await and TPL. Below are the results specifying which machine ran the client and what are the server process resources allocated.

| Client machine | Clients succeeded | Client time | Server machine | Server CPU | Server threads | Server RAM |
| :------------- | :---------------- | :---------- | :------------- | :--------- | :------------- | :--------- |
| Moderate       | 15869             | 21 seconds  | Weaker         | 150%-200%  | ?              | 1.3GB      |
| Weaker         | 28232             | 145 seconds | Moderate       | 40-50%     | 67             | 3.55GB     |

The number of clients that succeeded is the number of clients that were able to connect. That number is a result of port exhaustion limits hit on both OS-es which ran the clients. The Windows error was `An operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full` and the Ubuntu Server one was `Cannot assign requested address`. Additionally when clients were on the weaker machine the client time required in order to complete the requests is considerably higher while it was obvious that the server could handle more load. One of these could be the issue: Windows 10 has some limits which prohibit it from handling a high number of concurrent clients, the weaker machine is really weak, there is a concurrency issue with .NET 5 at least on Ubuntu Server, the client code could be improved to better schedule the tasks instead of relying on async-await and TPL.

The client-side limitation will be mitigated by the fact that each client is typically on a separate device. Therefore the server-side limitations remaining are the resources allocated for each connected client. Based on these numbers I think that **64 k concurrent connections** could be a realistic number for a messaging server and would be a useful limit in our calculations. It is a round number close to the **65535 max number of ports** for the current OS-es.

# Back of the envelope calculations

The [back-of-the-envelope](01-intro-02-back-of-the-envelope.md) file contains the detailed calculations. A messaging server is the server to which users directly connect to. A key limit is **64 k connections per messaging server**. A simple calculation tells that **1.6 k messaging servers** are needed in order to support **100 mln active users**. We would consider **256 bytes message size**.

Calculating the daily usage with **640 mln users** spread throughout the day each of which sends **128 messages per day** gives us **950 000 messages/s for the cell** and **232 MB/s for the cell** with **0.15 MB/s per messaging server**.

Calculating a peak usage for **1 hour** daily where **80%** of the maximum users - **80 mln active users** send **50%** of their daily messages - **64 messages** we get **1 423 000 messages/s for the cell** and **348 MB/s for the cell** with **0.22 MB/s per messaging server**.

These numbers do not take into account the security and transport data overhead. Additionally, this traffic would be multiplied. For example sending a message would require that data to be passed between different layers, possibly to multiple recipients and stored multiple times in order to enable faster querying.

Numbers are not small when we look at the cell as a whole. But for a single messaging server the throughput is tiny. Using linearly scalable technologies would allow us to achieve the desired throughput.
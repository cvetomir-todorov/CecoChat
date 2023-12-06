# Concurrent connections limit

Below is a benchmark for the number of concurrent connections per messaging server. The code is in `Check.sln`.

# Hardware setup

I used two machines connected via 100Mbps router.

| Machine  | CPU         | Frequency | Cores | RAM  | OS                      |
|:---------|:------------|:----------|:------|:-----|:------------------------|
| Weaker   | Core 2 Duo  | 2133MHz   | 2     | 4GB  | Ubuntu Server 20.04 LTS |
| Moderate | QuadCore i5 | 3533MHz   | 4     | 16GB | Windows 10 20H2         |

# Scenario and results

On the server I am using gRPC services hosted by ASP.NET Core which utilize .NET async programming model. The clients connect first and then simultaneously start sending 20 messages at a rate of 1 per second. Below are the results specifying which machine ran the client and what are the server process resources allocated.

| Client machine | Clients succeeded | Client time | Server machine | Server CPU | Server threads | Server RAM |
|:---------------|:------------------|:------------|:---------------|:-----------|:---------------|:-----------|
| Moderate       | 15869             | 21 seconds  | Weaker         | 150%-200%  | ?              | 1.3GB      |
| Weaker         | 28232             | 145 seconds | Moderate       | 40-50%     | 67             | 3.55GB     |

# Analysis

The `Clients succeeded` is the number of clients that were able to connect. That number is a result of port exhaustion limits hit on both OS-es which **ran the clients**, not the servers. The Windows error was `An operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full`. The Ubuntu Server one was `Cannot assign requested address`.

Strangely when clients were on the weaker machine the client time required in order to complete the requests is considerably higher while it was obvious that the server could handle more load. One of these could be the issue: the client code could be improved to manually schedule the tasks instead of relying on the .NET async programming model, the weaker machine is really weak, Windows 10 has some limits which prohibit it from handling a high number of concurrent clients, there is a concurrency issue with .NET on Ubuntu Server.

# Conclusion

The client-side port exhaustion limitation will be mitigated by the fact that each client is typically on a separate device. Therefore the server-side limitations remaining are the resources allocated for each connected client. Based on these numbers **64 k concurrent connections** per messaging server could be a realistic number and would be a useful limit in the calculations. It is a round number close to the **65535 max number of ports** for the current OS-es.

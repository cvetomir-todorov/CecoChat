# Overall design

![Overall design](images/cecochat-overall.png)

* Clients register and manage their profile in the User service via the BFF
* Clients create their session using the User service via the BFF
* Clients acquire other users public profiles from the User service via the BFF
* Clients manage their connections - invite/accept/cancel/remove from the User service via the BFF
* Clients upload/download their files to/from the File storage via the BFF
* Clients get the list of their files from the User service via the BFF
* Clients acquire user chats and chat history from the Chats service via the BFF
* Clients connect to Messaging service in order to chat with each other
* Messaging service instances exchange data between each other indirectly using a PUB/SUB backplane
* PUB/SUB backplane distributes the traffic between Messaging service instances
* ID Gen service is used to generate Snowflake message IDs
* Chats service transforms the messages from the PUB/SUB backplane into the Chats database which is the source of truth 
* Chats service transforms the messages and reactions from the PUB/SUB backplane into the Chats database which is the source of truth

In terms of development and operations:

* Splitting the different responsibilities between separate services enables independent development, testing, versioning, deployment, scaling
* The services use dynamic configuration which is updated centrally
* Observability is achieved via health checks, distributed tracing, log aggregation and metrics with monitoring
* Deployment infrastructure takes care of load balancing, auto-scaling

More details on the design of each aspect:

* [Messaging](design-messaging.md)
* [Chats](design-chats.md)
* [User profiles and connections](design-users.md)
* [Clients](design-clients.md)
* [Configuration](design-configuration.md)
* [Observability](design-observability.md)
* [Deployment](design-deployment.md)

All the diagrams are in the [diagrams](diagrams) folder and [draw.io](https://app.diagrams.net/) is needed in order to view them. From the `Help` item in the menu a desktop tool could be downloaded, if preferred. Currently this is the [link with the releases](https://github.com/jgraph/drawio-desktop/releases).

# Configuration

## Static

Static app configuration is stored in `appsettings.json` files as is typical for .NET applications. For some servers it is overriden for the `Development` environment via the `appsettings.ENVIRONMENT.json` approach.

The [CecoChat docker-compose file](../run/cecochat.yml) uses environment variables with specific prefixes in order to push static configuration values to the servers at start-up. [ASP.NET documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-5.0#environment-variables) describes how to name the environment variables in order to override both ASP.NET and app-specific values.

## Dynamic

Some parts of the configuration are designed to be changed while it is running. These parts are stored in Redis and read at start-up. The [prepare script](../run/redis/redis-create-configuration.sh) inputs the partitioning and history configuration values which need to be present and valid at all times. The configuration can be changed manually using the `redis-cli` command after attaching to the Redis container via `docker exec -it cecochat-redis1 bash`. Each server that is using dynamic configuration outputs at start-up the key names it reads and PUB/SUB channels which it subscribes to in order to get notified about changes. After the configuration is changed a dummy message needs to be sent to one of those PUB/SUB channels. The servers validate the new configuration values and may reject the change. The validation output provides descriptive information which can be used to correct the values.

In the future there may be a web-based Configurator which uses a Redis client and would allow changing the configuration more easily.

# Design

![Configuration](images/cecochat-06-configuration.png)

The configuration database stores information related to server partition assignment. Each messaging server is assigned a server ID. The configuration database stores the address and partitions for each server ID. It is used by:

* BFF service to:
  - Get the user partition by the user ID
  - Get the server address for that partition
* Messaging service to:
  - Assign the partitions to the Kafka consumer in order to consume messages

Redis conveniently supports simple keys for plain data such as partition count and history settings. Redis hashes (hash maps) are used to store key-value pairs like the `(server ID -> partitions)` and `(server ID -> address)`. After the configurator server applies configuration changes the Redis PUB/SUB is used to publish a notification to subscribers using the respective channels. Each interested server listens to changes for:

* BFF service - partition count, server partitions, server addresses
* Messaging service - partition count, server partitions
* History service - history settings

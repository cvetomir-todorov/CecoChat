# Message IDs

Each message needs to have a unique ID. Additionally it is associated with a timestamp. There are a few ways to do it.

# UUID/GUID + timestamp

UUID/GUID instances are globally unique and easily generated. Timestamps are easily generated as well. Unfortunately these two have larger size of 16 bytes for UUID/GUID and 8 bytes for timestamps. That means 24 bytes in total.

Cassandra is a KKV database. The V stands for value and the KK means that there are 2 keys. The first K is a partitioning key and indicates which partition the data is in. We would use the user ID or chat ID as Cassandra partitioning key. The second K is a clustering key and is used to physically sort the data in that partition. Since we want to make range queries by time it makes sense to use the timestamp as Cassandra clustering key.

With these indexes in place if we want to make a query by message ID we won't be able to do so in Cassandra as there's no such an index. We can only make such a query by the clustering key which is a timestamp. Using timestamps instead of message IDs is dangerous due to their possible duplication and different representation in different systems. Ideally we would want to use message IDs which are guaranteed to be unique and are simpler. Unfortunately UUID/GUIDs are not simple. And even if they were we would need a secondary index for that kind of query. Cassandra secondary indexes are dangerous as they are spread throughout the nodes in the cluster. That means a query using a secondary index could affect much more nodes that we want to, reducing performance in result.

# Snowflake

Twitter snowflakes are 8-byte or int64 IDs which higher bits contain timestamp value. That means they are time sortable. Using snowflakes as time-sortable message IDs allow us to use them in Cassandra as a clustering key. That way we can make ranged time-based queries and also make queries by message ID. Since snowflakes are int64 their representation is determinate in different systems. Apart from the sign bit maybe, which is why some libs do not use it.

In order to fit more timestamps snowflakes use an epoch - `timestamp = epoch-ticks + snowflake-ticks`. There are 2 problems though in a system that is highly scalable and distributed.
* We need to generate more than 1 ID during a snowflake tick. In order to solve this problem part of the bits in the snowflake are used as a **sequence**. This means that for a given snowflake tick we have a number of IDs and only when they end we would need to wait for a new tick to occur.
* We also need to generate IDs in a distributed way instead of a centralized way. To solve this problem part of the bits in the snowflake are used as a **generator ID**. This way we can use independent servers, processes, threads to generate IDs.

The way snowflakes are used is - `41 timestamp bits + 8 generator bits + 14 sequence bits`. That means we have up to **256 generators**, each of which can generate up to **16384 IDs** per snowflake tick. We're using **1 ms tick** which means that we have close to **70 years interval**.

If we generated message IDs in messaging service we would need a lot of generator IDs since we have a lot of messaging servers. In order to assign less bits for the generator ID we introduce a dedicated ID Gen service. This introduces an additional element in the system and increases the operation complexity. The latency is also increased since messaging service now need to obtain an ID from the ID Gen service. When the IDs are requested in a batch though that effect is reduced at the cost of some additional complexity in the implementation.

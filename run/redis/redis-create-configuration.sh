# partitioning
redis-cli SET   partitioning.partition-count 12
redis-cli HMSET partitioning.server-partitions 1 0-5 2 6-11
redis-cli HMSET partitioning.server-addresses 1 https://localhost:31001 2 https://localhost:31011

# history
redis-cli SET history.chat.message-count 32

# snowflake
redis-cli HSET snowflake.server-generator-ids 1 1,2,3,4

# partitioning
redis-cli SET   partitioning.partition-count 12
redis-cli HMSET partitioning.server-partitions 0 "0-5" 1 "6-11"
redis-cli HMSET partitioning.server-addresses 0 https://TODO 1 https://TODO

# history
redis-cli SET history.chat.message-count 32

# snowflake
redis-cli HSET snowflake.server-generator-ids 0 "0,1" 2 "2,3"

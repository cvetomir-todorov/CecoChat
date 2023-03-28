# partitioning
redis-cli SET   partitioning.partition-count 12
redis-cli HMSET partitioning.server-partitions 0 "0-5" 1 "6-11"
redis-cli HMSET partitioning.server-addresses 0 "https://messaging.cecochat.com/m0" 1 "https://messaging.cecochat.com/m1"

# history
redis-cli SET history.chat.message-count 32

# snowflake
redis-cli HSET snowflake.server-generator-ids 0 "0,1" 1 "2,3"
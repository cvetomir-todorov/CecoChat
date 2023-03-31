# partitioning
redis-cli SET   partitioning.count 12
redis-cli HMSET partitioning.partitions 0 "0-5" 1 "6-11"
redis-cli HMSET partitioning.addresses 0 "https://localhost:31001" 1 "https://localhost:31011"

# history
redis-cli SET history.message-count 32

# snowflake
redis-cli HSET snowflake.generator-ids 0 "1,2,3,4"

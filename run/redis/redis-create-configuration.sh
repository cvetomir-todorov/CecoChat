# partitioning
redis-cli SET   partitioning.partition-count 360
redis-cli HMSET partitioning.server-partitions s1 0-179 s2 180-359
redis-cli HMSET partitioning.server-addresses s1 https://localhost:31001 s2 https://localhost:31011

# history
redis-cli SET history.server-address https://localhost:31003
redis-cli SET history.user.message-count 64
redis-cli SET history.dialog.message-count 32

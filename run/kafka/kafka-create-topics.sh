kafka-topics --zookeeper kafka1:9091 --create --topic messages-by-receiver --partitions 12 --replication-factor 2 --config min.insync.replicas=2
kafka-topics --zookeeper kafka1:9091 --create --topic messages-by-sender --partitions 12 --replication-factor 2 --config min.insync.replicas=2

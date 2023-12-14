kafka-topics --bootstrap-server backplane-kafka-0.backplane-kafka.cecochat.svc.cluster.local:9092 --create --topic messages-by-receiver --partitions 12 --replication-factor 2 --config min.insync.replicas=2
kafka-topics --bootstrap-server backplane-kafka-0.backplane-kafka.cecochat.svc.cluster.local:9092 --create --topic config-changes --partitions 1 --replication-factor 2 --config min.insync.replicas=2
kafka-topics --bootstrap-server backplane-kafka-0.backplane-kafka.cecochat.svc.cluster.local:9092 --create --topic health --partitions 1 --replication-factor 1
kafka-topics --bootstrap-server backplane-kafka-0.backplane-kafka.cecochat.svc.cluster.local:9092 --list

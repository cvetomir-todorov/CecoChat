# Deployment

![Deployment](images/cecochat-07-deployment.png)

## Auto-scaling

BFF, History, ID Gen services are stateless which makes this fairly trivial. The deployment infrastructure adds a new instance and the load balancer takes it into account when a new request comes in.

Messaging, State services are stateful. Their state is the Kafka partitions which they are consuming. Therefore the auto-scaling means creating new service instances and distributing some of the partitions to the new instances. All this data is stored in the dynamic configuration which notifies all alive instances.

Example with messaging servers:
* Kafka partitions are 0-359
* Messaging servers handling those partitions are `s1 -> [0-179]`, `s2 -> [180-359]`
* When the load on them is too big some partitions could be redistributed to a new messaging server s3
* Now the distribution is `s1 -> [0-119]`, `s2 -> [180-299]`, `s3 -> [120-179, 300-359]`

The number of partitions can be big. Kafka clusters are known to work with 10 000 or 20 000 partitions. Which means their distribution can be made even with the appropriate number. Additionally the partitions can be split into smaller partition ranges in order to make it more conventient to create an algorithm for even distribution between the instances.

## Failover

BFF, History, ID Gen services are stateless so if an instance dies the requests are sent to one of the other services via the load balancers. In the meantime the deployment infrastructure keeps the desired number of instances by creating a new one.

Messaging, State services are stateful. If an instance dies the deployment infrastructure would create a new one. In order to speed things up though it can keep a few instances idle. This way the wait interval will be greatly reduced if the time for a new instance to be created is big.
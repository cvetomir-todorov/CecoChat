# Minikube deployment

Make sure that the [prerequisites](dev-run-prerequisites.md) have been met before continuing.

## Instances

* Integration
  - Kafka - 2 Zookeepers, 2 brokers
* Data storage
  - YugabyteDB - 2 masters, 2 tservers
  - Cassandra - 2 nodes
  - Redis - 3 masters
* Observability
  - Telemetry - 2 OTel collector
  - Tracing - 1 Jaeger all-in-one
  - Logging - 1 ElasticSearch, 1 Kibana
* CecoChat
  - BFF branch - 2 BFF, 2 User, 2 Chats
  - Messaging branch - 2 Messaging, 2 IDGen

## Tools

* `minikube`
  - Started using `minikube start --container-runtime=docker --kubernetes-version=stable`
  - A previous minikube profile could be cleaned up using `minikube delete` 
* Minikube addons listed in `enable-minikube-addons.sh`
* `kubectl`, `kubens`, `helm`

## Deployment

The below commands assume:
* The current dir is `/deploy/minikube`
* The containers are built into `minikube` docker repo
* `minikube` is started with the addons enabled

It should be taken into account that some of the docker images needed to be pulled are not small. Some of the 3rd party components and all of the CecoChat components have health probes. They could be used to ensure the component is running before moving onto the next one. Commands like `kubectl get all`, `kubectl describe pod <pod-name>`, `kubectl logs <pod-name>` could help inspect and troubleshoot a pod.

### Initial

First and foremost the namespace needs to be created and the context needs to be switched to it:
```shell
kubectl apply -f namespace.yml
kubens cecochat
```

### Data and integration

```shell
bash install-data-integration.sh
```

Redis cluster is initialized manually by executing the content of the `cluster.sh` script from inside a Redis pod:
```shell
kubectl exec -it redis-0 -- bash
```

Initial dynamic config is created manually from inside the Redis pod using the content of the `data.sh` script and lives as long as the same `PersistentVolume` is used. Unfortunately, due to how Redis works, the contents of the script needs to be executed in each of the 3 instances of the cluster.
```shell
kubectl exec -it redis-0 -- bash
kubectl exec -it redis-1 -- bash
kubectl exec -it redis-2 -- bash
```

The topics in Kafka and databases in Cassandra and Yugabyte are created when the respective service is started, if they do not exist.

### Observability

```shell
bash install-telemetry.sh
```

### Services shared resources

* The certificate scripts rely on the correct working dir which require go-in-go-out approach
* Appending the ingress certificate hostnames to `/etc/hosts` is required in order to be able to access the cluster
* Creating the shared by the services `ConfigMap` instances is required before installing them

```shell
cd certificates
bash create-tls-secret.sh
bash append-to-etc-hosts.sh
cd ..
kubectl apply -f service-config.yml
```

### Services

```shell
bash install-cecochat.sh
```

## Cluster access

The Kubernetes ingresses expose the following services via the respective domains:
* `https://bff.cecochat.com` -> BFF service
* `https://messaging.cecochat.com` -> Messaging service
* `https://jaeger.cecochat.com` -> traces in Jaeger
* `https://kibana.cecochat.com` -> logs in Kibana

Commands useful when troubleshooting and investigating issues include:
* `kubectl port-forward <pod-name> <machine-port>:<pod-port>` - temporarily gain access to some component
* `kubectl describe configmap <configmap-name>` - check what the values in a configmap are
* `kubectl logs <pod-name>` - show the logs for a given pod
* `kubectl exec -it <pod-name> -- bash` - log into a pod, investigate issues, check values of env vars, etc.
* `curl https://bff.cecochat.com/healthz -v` - investigate any issues with the ingress certificate

## Validate deployment

```shell
$ helm list
NAME     	NAMESPACE	REVISION	UPDATED                                	STATUS  	CHART          	APP VERSION
backplane	cecochat 	1       	2023-12-04 18:09:22.658051056 +0200 EET	deployed	backplane-0.1.0	0.1.0      
bff      	cecochat 	1       	2023-12-04 18:21:25.699465741 +0200 EET	deployed	bff-0.1.0      	0.1.0      
cassandra	cecochat 	1       	2023-12-04 18:09:22.900641214 +0200 EET	deployed	cassandra-0.1.0	0.1.0      
chats   	cecochat 	1       	2023-12-04 18:19:07.051280032 +0200 EET	deployed	chats-0.1.0  	0.1.0      
idgen    	cecochat 	1       	2023-12-04 18:14:52.595844209 +0200 EET	deployed	idgen-0.1.0    	0.1.0      
logging  	cecochat 	1       	2023-12-04 18:11:35.508839327 +0200 EET	deployed	logging-0.1.0  	0.1.0      
messaging	cecochat 	1       	2023-12-04 18:22:32.64885057 +0200 EET 	deployed	messaging-0.1.0	0.1.0      
redis    	cecochat 	1       	2023-12-04 18:09:22.333514145 +0200 EET	deployed	redis-0.1.0    	0.1.0      
telemetry	cecochat 	1       	2023-12-04 18:13:05.904910673 +0200 EET	deployed	telemetry-0.1.0	0.1.0      
tracing  	cecochat 	1       	2023-12-04 18:09:30.784510949 +0200 EET	deployed	tracing-0.1.0  	0.1.0      
user     	cecochat 	1       	2023-12-04 18:15:07.401506135 +0200 EET	deployed	user-0.1.0     	0.1.0      
yb       	cecochat 	1       	2023-12-04 18:09:23.08036877 +0200 EET 	deployed	yugabyte-0.1.0 	0.1.0      
```

```shell
$ kubectl get pod
NAME                                        READY   STATUS    RESTARTS      AGE
backplane-kafka-0                           1/1     Running   2 (19m ago)   20m
backplane-kafka-1                           1/1     Running   2 (19m ago)   20m
backplane-zk-0                              1/1     Running   0             20m
backplane-zk-1                              1/1     Running   0             20m
bff-767787c978-9g8xl                        1/1     Running   0             7m59s
bff-767787c978-gclk9                        1/1     Running   0             7m59s
cassandra-0                                 1/1     Running   0             20m
cassandra-1                                 1/1     Running   0             20m
chats-685876fb4f-2zq99                      1/1     Running   0             10m
chats-685876fb4f-b2twz                      1/1     Running   0             10m
idgen-0                                     1/1     Running   0             14m
idgen-1                                     1/1     Running   0             14m
logging-es-0                                1/1     Running   0             17m
logging-kibana-0                            1/1     Running   0             17m
messaging-0                                 1/1     Running   0             6m52s
messaging-1                                 1/1     Running   0             6m52s
redis-0                                     1/1     Running   0             20m
redis-1                                     1/1     Running   0             20m
redis-2                                     1/1     Running   0             20m
telemetry-otel-collector-6954b7b587-2mfkf   1/1     Running   0             16m
telemetry-otel-collector-6954b7b587-6xf6p   1/1     Running   0             16m
tracing-jaeger-0                            1/1     Running   0             19m
user-7f48d9b4d4-gj5gg                       1/1     Running   0             14m
user-7f48d9b4d4-q7g4s                       1/1     Running   0             14m
yb-master-0                                 1/1     Running   0             20m
yb-master-1                                 1/1     Running   0             20m
yb-tserver-0                                1/1     Running   0             20m
yb-tserver-1                                1/1     Running   0             20m
```

```shell
$ kubectl get service
NAME                       TYPE        CLUSTER-IP       EXTERNAL-IP   PORT(S)                               AGE
backplane-kafka            ClusterIP   None             <none>        9092/TCP                              20m
backplane-zk               ClusterIP   None             <none>        2181/TCP,2888/TCP,3888/TCP,8080/TCP   20m
bff                        ClusterIP   10.100.214.108   <none>        443/TCP                               8m16s
cassandra                  ClusterIP   None             <none>        9042/TCP                              20m
chats                      ClusterIP   10.104.95.42     <none>        443/TCP                               10m
idgen                      ClusterIP   None             <none>        443/TCP                               14m
logging-es                 ClusterIP   None             <none>        9200/TCP                              18m
logging-kibana             ClusterIP   None             <none>        5601/TCP                              18m
messaging                  ClusterIP   None             <none>        443/TCP                               7m9s
messaging-0                ClusterIP   10.110.160.166   <none>        443/TCP                               7m9s
messaging-1                ClusterIP   10.110.226.223   <none>        443/TCP                               7m9s
redis                      ClusterIP   None             <none>        6379/TCP                              20m
telemetry-otel-collector   ClusterIP   10.103.130.30    <none>        4317/TCP                              16m
tracing-jaeger             ClusterIP   None             <none>        4317/TCP,5778/TCP,16686/TCP           20m
user                       ClusterIP   10.98.228.227    <none>        443/TCP                               14m
yb-master                  ClusterIP   None             <none>        7000/TCP,7100/TCP                     20m
yb-tserver                 ClusterIP   None             <none>        5433/TCP,9000/TCP,9100/TCP            20m
```

```shell
$ kubectl get deployments
NAME                       READY   UP-TO-DATE   AVAILABLE   AGE
bff                        2/2     2            2           8m31s
chats                      2/2     2            2           10m
telemetry-otel-collector   2/2     2            2           16m
user                       2/2     2            2           14m
```

```shell
$ kubectl get replicasets
NAME                                  DESIRED   CURRENT   READY   AGE
bff-767787c978                        2         2         2       8m48s
chats-685876fb4f                      2         2         2       11m
telemetry-otel-collector-6954b7b587   2         2         2       17m
user-7f48d9b4d4                       2         2         2       15m
```

```shell
$ kubectl get statefulsets
NAME              READY   AGE
backplane-kafka   2/2     21m
backplane-zk      2/2     21m
cassandra         2/2     21m
idgen             2/2     15m
logging-es        1/1     18m
logging-kibana    1/1     18m
messaging         2/2     7m56s
redis             3/3     21m
tracing-jaeger    1/1     20m
yb-master         2/2     21m
yb-tserver        2/2     21m
```

```shell
$ kubectl get ingress
NAME             CLASS   HOSTS                    ADDRESS        PORTS     AGE
bff              nginx   bff.cecochat.com         192.168.49.2   80, 443   9m21s
logging-kibana   nginx   kibana.cecochat.com      192.168.49.2   80, 443   19m
messaging        nginx   messaging.cecochat.com   192.168.49.2   80, 443   8m14s
tracing-jaeger   nginx   jaeger.cecochat.com      192.168.49.2   80, 443   21m
```

```shell
$ kubectl get pvc
NAME                     STATUS   VOLUME                                     CAPACITY   ACCESS MODES   STORAGECLASS   AGE
data-backplane-kafka-0   Bound    pvc-4c7e8aec-8826-401f-824e-59d8be53023e   512Mi      RWO            standard       6d22h
data-backplane-kafka-1   Bound    pvc-9bffdb58-9fca-47d5-bacf-7524537489ac   512Mi      RWO            standard       6d22h
data-backplane-zk-0      Bound    pvc-bf06aff7-5ef6-4c6a-a818-af457bfaf6c4   128Mi      RWO            standard       6d22h
data-backplane-zk-1      Bound    pvc-87899679-02d6-4195-9f2d-7a4064cba59a   128Mi      RWO            standard       6d22h
data-cassandra-0         Bound    pvc-f94f74de-008f-446f-8003-f44cf2b65d4b   1Gi        RWO            standard       2d6h
data-cassandra-1         Bound    pvc-2aa65930-bef4-4e85-8bab-c5abb0568456   1Gi        RWO            standard       2d6h
data-logging-es-0        Bound    pvc-9f6817a1-5f5c-4961-ba8f-c16f57c24cf9   1Gi        RWO            standard       6h39m
data-logging-kibana-0    Bound    pvc-f4946348-2db1-48d7-b489-911e577c5190   512Mi      RWO            standard       6h39m
data-redis-0             Bound    pvc-acff54d4-b6f5-4dbf-a972-a9651a8d23df   128Mi      RWO            standard       21m
data-redis-1             Bound    pvc-167bd289-3ebc-4974-b59a-222e364d1f8a   128Mi      RWO            standard       21m
data-redis-2             Bound    pvc-6c2a395d-fafd-4aed-988b-2bdbabb54350   128Mi      RWO            standard       21m
data-yb-master-0         Bound    pvc-b15a98a4-9364-4c54-bbd0-6d4eccdc382a   1Gi        RWO            standard       2d6h
data-yb-master-1         Bound    pvc-c32d9aa7-69b2-44aa-969a-9f6eef62a617   1Gi        RWO            standard       2d6h
data-yb-tserver-0        Bound    pvc-8015a1c6-b6af-4692-8abc-1075e8d0e40d   1Gi        RWO            standard       2d6h
data-yb-tserver-1        Bound    pvc-5fbe39b2-628c-485d-a418-ced1345ca0ec   1Gi        RWO            standard       2d6h
```

```shell
$ kubectl get configmaps
NAME                       DATA   AGE
backplane-kafka            1      22m
backplane-zk               1      22m
cassandra-env              8      22m
cassandra-scripts          2      22m
idgen                      1      16m
messaging                  1      8m54s
redis                      1      22m
service-aspnet             6      22m
service-backplane          2      22m
service-configdb           6      22m
service-logging            2      22m
service-tracing            4      22m
telemetry-otel-collector   1      18m
```

```shell
$ kubectl get secrets
NAME                              TYPE                 DATA   AGE
ingress-tls                       kubernetes.io/tls    2      22m
```

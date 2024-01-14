# Minikube deployment

Make sure that the [prerequisites](dev-run-prerequisites.md) have been met before continuing.

# Instances

* Integration
  - Kafka - 2 Zookeepers, 2 brokers
* Data storage
  - YugabyteDB - 2 masters, 2 tservers
  - Cassandra - 2 nodes
  - Redis - 3 masters
  - MinIO - 1 node
* Observability
  - Telemetry - 2 OTel collectors
  - Tracing - 1 Jaeger all-in-one
  - Logging - 1 ElasticSearch, 1 Kibana
* CecoChat
  - Config branch - 1 Config
  - BFF branch - 2 BFF, 2 User, 2 Chats
  - Messaging branch - 2 Messaging, 2 IDGen

# Tools

* `minikube`
  - Started using `minikube start --container-runtime=docker --kubernetes-version=stable`
  - A previous minikube profile could be cleaned up using `minikube delete` 
* Minikube addons listed in `enable-minikube-addons.sh`
* `kubectl`, `kubens`, `helm`

# Deployment

The below commands assume:
* The current dir is `/deploy/minikube`
* The containers are built into `minikube` docker repo
* `minikube` is started with the addons enabled

It should be taken into account that some of the docker images needed to be pulled are not small. Some of the 3rd party components and all of the CecoChat components have health probes. They could be used to ensure the component is running before moving onto the next one. Commands like `kubectl get all`, `kubectl describe pod <pod-name>`, `kubectl logs <pod-name>` could help inspect and troubleshoot a pod.

## Namespace

First and foremost the namespace needs to be created and the context needs to be switched to it:
```shell
kubectl apply -f namespace.yml
kubens cecochat
```

## TLS secrets and hostnames

* The certificate scripts rely on the correct working dir which require go-in-go-out approach
* Appending the ingress certificate hostnames to `/etc/hosts` is required in order to be able to access the cluster

```shell
cd certificates
bash create-tls-secret.sh
bash append-to-etc-hosts.sh
cd ..
```

## Data and integration

```shell
bash install-data-integration.sh
```

Redis cluster is initialized manually by executing the content of the `cluster.sh` script from inside a Redis pod:
```shell
kubectl exec -it redis-0 -- bash
```

The topics in Kafka, the keyspaces in Cassandra, the databases in Yugabyte, the buckets in MinIO, are all created when the respective service is started, if they do not exist.

## Observability

```shell
bash install-telemetry.sh
```

## Static configuration

Creating the shared by the services `ConfigMap` instances is required before installing them

```shell
kubectl apply -f service-config.yml
```

## Dynamic configuration

Dynamic configuration is something which all application services depend on

```shell
bash install-config.sh
```

## Services

```shell
bash install-cecochat.sh
```

# Cluster access

The Kubernetes ingresses expose the following services via the respective domains:

* Operations
  - `https://config.cecochat.com/swagger` - the dynamic configuration Swagger interface 
  - `https://jaeger.cecochat.com` - traces in Jaeger
  - `https://kibana.cecochat.com` - logs in Kibana
* Data
  - `https://minio.cecochat.com` - MinIO files
* Client
  - `https://bff.cecochat.com` - BFF service
  - `https://messaging.cecochat.com` - Messaging service

Commands useful when troubleshooting and investigating issues include:
* `kubectl port-forward <pod-name> <machine-port>:<pod-port>` - temporarily gain access to some component
* `kubectl describe configmap <configmap-name>` - check what the values in a configmap are
* `kubectl logs <pod-name>` - show the logs for a given pod
* `kubectl exec -it <pod-name> -- bash` - log into a pod, investigate issues, check values of env vars, etc.
* `curl https://bff.cecochat.com/healthz -v` - investigate any issues with the ingress certificate

# Validate deployment

```shell
$ helm list
NAME     	NAMESPACE	REVISION	UPDATED                                	STATUS  	CHART          	APP VERSION
backplane	cecochat 	1       	2023-12-19 11:19:43.275872281 +0200 EET	deployed	backplane-0.1.0	0.1.0      
bff      	cecochat 	1       	2023-12-19 12:11:16.477186671 +0200 EET	deployed	bff-0.1.0      	0.1.0      
cassandra	cecochat 	1       	2023-12-19 11:19:55.588187972 +0200 EET	deployed	cassandra-0.1.0	0.1.0      
chats    	cecochat 	1       	2023-12-19 12:11:17.202963604 +0200 EET	deployed	chats-0.1.0    	0.1.0      
config   	cecochat 	1       	2023-12-19 12:08:51.637466328 +0200 EET	deployed	config-0.1.0   	0.1.0      
idgen    	cecochat 	1       	2023-12-19 12:11:17.040213258 +0200 EET	deployed	idgen-0.1.0    	0.1.0      
logging  	cecochat 	1       	2023-12-19 12:43:29.96052056 +0200 EET 	deployed	logging-0.1.0  	0.1.0      
messaging	cecochat 	1       	2023-12-19 12:11:16.811841518 +0200 EET	deployed	messaging-0.1.0	0.1.0      
minio   	cecochat 	1       	2023-12-19 12:13:26.551841517 +0200 EET	deployed	minio-0.1.0	    0.1.0      
redis    	cecochat 	1       	2023-12-19 11:24:25.946219644 +0200 EET	deployed	redis-0.1.0    	0.1.0      
telemetry	cecochat 	1       	2023-12-19 12:43:29.632952485 +0200 EET	deployed	telemetry-0.1.0	0.1.0      
tracing  	cecochat 	1       	2023-12-19 12:43:29.798445504 +0200 EET	deployed	tracing-0.1.0  	0.1.0      
user     	cecochat 	1       	2023-12-19 12:11:16.667136273 +0200 EET	deployed	user-0.1.0     	0.1.0      
yb       	cecochat 	1       	2023-12-19 11:20:17.985851232 +0200 EET	deployed	yugabyte-0.1.0 	0.1.0 
```

```shell
$ kubectl get pod
NAME                                        READY   STATUS    RESTARTS      AGE
backplane-kafka-0                           1/1     Running   0             85m
backplane-kafka-1                           1/1     Running   0             85m
backplane-zk-0                              1/1     Running   0             85m
backplane-zk-1                              1/1     Running   0             85m
bff-6dd88cdc4f-687ls                        1/1     Running   0             33m
bff-6dd88cdc4f-wq4nx                        1/1     Running   0             33m
cassandra-0                                 1/1     Running   0             85m
cassandra-1                                 1/1     Running   0             85m
chats-69859f9cb5-hnpls                      1/1     Running   0             33m
chats-69859f9cb5-ngzhx                      1/1     Running   0             33m
config-7fff897656-6zvv5                     1/1     Running   0             36m
config-7fff897656-fmsr8                     1/1     Running   0             36m
idgen-0                                     1/1     Running   0             33m
idgen-1                                     1/1     Running   0             33m
logging-es-0                                1/1     Running   0             88s
logging-kibana-0                            1/1     Running   0             88s
messaging-0                                 1/1     Running   0             33m
messaging-1                                 1/1     Running   0             33m
minio-0                                     1/1     Running   0             37m
redis-0                                     1/1     Running   0             80m
redis-1                                     1/1     Running   0             80m
redis-2                                     1/1     Running   0             80m
telemetry-otel-collector-6954b7b587-khgvz   1/1     Running   0             89s
telemetry-otel-collector-6954b7b587-vhr58   1/1     Running   0             89s
tracing-jaeger-0                            1/1     Running   0             89s
user-6ccb5c897-2qckk                        1/1     Running   0             33m
user-6ccb5c897-xjgqf                        1/1     Running   0             33m
yb-master-0                                 1/1     Running   0             84m
yb-master-1                                 1/1     Running   0             84m
yb-tserver-0                                1/1     Running   0             84m
yb-tserver-1                                1/1     Running   0             84m
```

```shell
$ kubectl get service
NAME                       TYPE        CLUSTER-IP       EXTERNAL-IP   PORT(S)                               AGE
backplane-kafka            ClusterIP   None             <none>        9092/TCP                              85m
backplane-zk               ClusterIP   None             <none>        2181/TCP,2888/TCP,3888/TCP,8080/TCP   85m
bff                        ClusterIP   10.107.196.138   <none>        443/TCP                               34m
cassandra                  ClusterIP   None             <none>        9042/TCP                              85m
chats                      ClusterIP   10.105.229.37    <none>        443/TCP                               34m
config                     ClusterIP   10.96.97.86      <none>        443/TCP                               36m
idgen                      ClusterIP   None             <none>        443/TCP                               34m
logging-es                 ClusterIP   None             <none>        9200/TCP                              2m7s
logging-kibana             ClusterIP   None             <none>        5601/TCP                              2m7s
messaging                  ClusterIP   None             <none>        443/TCP                               34m
messaging-0                ClusterIP   10.97.159.51     <none>        443/TCP                               34m
messaging-1                ClusterIP   10.100.37.180    <none>        443/TCP                               34m
minio                      ClusterIP   None             <none>        9000/TCP,9090/TCP                     37m
redis                      ClusterIP   None             <none>        6379/TCP                              81m
telemetry-otel-collector   ClusterIP   10.109.140.197   <none>        4317/TCP                              2m8s
tracing-jaeger             ClusterIP   None             <none>        4317/TCP,5778/TCP,16686/TCP           2m8s
user                       ClusterIP   10.109.128.156   <none>        443/TCP                               34m
yb-master                  ClusterIP   None             <none>        7000/TCP,7100/TCP                     85m
yb-tserver                 ClusterIP   None             <none>        5433/TCP,9000/TCP,9100/TCP            85m
```

```shell
$ kubectl get deployments
NAME                       READY   UP-TO-DATE   AVAILABLE   AGE
bff                        2/2     2            2           34m
chats                      2/2     2            2           34m
config                     2/2     2            2           37m
telemetry-otel-collector   2/2     2            2           2m26s
user                       2/2     2            2           34m
```

```shell
$ kubectl get replicasets
NAME                                  DESIRED   CURRENT   READY   AGE
bff-6dd88cdc4f                        2         2         2       34m
chats-69859f9cb5                      2         2         2       34m
config-7fff897656                     2         2         2       37m
telemetry-otel-collector-6954b7b587   2         2         2       2m45s
user-6ccb5c897                        2         2         2       34m
```

```shell
$ kubectl get statefulsets
NAME              READY   AGE
backplane-kafka   2/2     86m
backplane-zk      2/2     86m
cassandra         2/2     86m
idgen             2/2     35m
logging-es        1/1     3m2s
logging-kibana    1/1     3m2s
messaging         2/2     35m
minio             1/1     37m
redis             3/3     82m
tracing-jaeger    1/1     3m3s
yb-master         2/2     86m
yb-tserver        2/2     86m
```

```shell
$ kubectl get ingress
NAME             CLASS   HOSTS                    ADDRESS        PORTS     AGE
bff              nginx   bff.cecochat.com         192.168.49.2   80, 443   35m
config           nginx   config.cecochat.com      192.168.49.2   80, 443   37m
logging-kibana   nginx   kibana.cecochat.com      192.168.49.2   80, 443   3m19s
messaging        nginx   messaging.cecochat.com   192.168.49.2   80, 443   35m
minio            nginx   minio.cecochat.com       192.168.49.2   80, 443   37m
tracing-jaeger   nginx   jaeger.cecochat.com      192.168.49.2   80, 443   3m20s
```

```shell
$ kubectl get configmaps
NAME                       DATA   AGE
backplane-kafka            1      88m
backplane-zk               1      88m
cassandra-env              8      87m
cassandra-scripts          2      87m
idgen                      1      36m
messaging                  1      36m
redis                      1      83m
service-aspnet             6      17d
service-backplane          2      17d
service-config-client      1      89m
service-logging            2      17d
service-tracing            4      17d
telemetry-otel-collector   1      4m17s
```

```shell
$ kubectl get secrets
NAME                              TYPE                 DATA   AGE
ingress-tls                       kubernetes.io/tls    2      17d
```

```shell
$ kubectl get pvc
NAME                     STATUS   VOLUME                                     CAPACITY   ACCESS MODES   STORAGECLASS   AGE
data-backplane-kafka-0   Bound    pvc-4c7e8aec-8826-401f-824e-59d8be53023e   512Mi      RWO            standard       21d
data-backplane-kafka-1   Bound    pvc-9bffdb58-9fca-47d5-bacf-7524537489ac   512Mi      RWO            standard       21d
data-backplane-zk-0      Bound    pvc-bf06aff7-5ef6-4c6a-a818-af457bfaf6c4   128Mi      RWO            standard       21d
data-backplane-zk-1      Bound    pvc-87899679-02d6-4195-9f2d-7a4064cba59a   128Mi      RWO            standard       21d
data-cassandra-0         Bound    pvc-f94f74de-008f-446f-8003-f44cf2b65d4b   1Gi        RWO            standard       17d
data-cassandra-1         Bound    pvc-2aa65930-bef4-4e85-8bab-c5abb0568456   1Gi        RWO            standard       17d
data-logging-es-0        Bound    pvc-9f6817a1-5f5c-4961-ba8f-c16f57c24cf9   1Gi        RWO            standard       15d
data-logging-kibana-0    Bound    pvc-f4946348-2db1-48d7-b489-911e577c5190   512Mi      RWO            standard       15d
data-minio-0             Bound    pvc-aa2bc4d4-cd32-4bd6-8040-cc90e544a2df   1Gi        RWO            standard       38m
data-redis-0             Bound    pvc-e9fbdc04-6b5b-48a4-a724-6d2f4f0516cd   128Mi      RWO            standard       82m
data-redis-1             Bound    pvc-5b28d789-1829-4bc9-8f48-c26d6e3d29d6   128Mi      RWO            standard       82m
data-redis-2             Bound    pvc-8f75ae4d-20fe-400e-acff-d8745cff42d7   128Mi      RWO            standard       82m
data-yb-master-0         Bound    pvc-b15a98a4-9364-4c54-bbd0-6d4eccdc382a   1Gi        RWO            standard       17d
data-yb-master-1         Bound    pvc-c32d9aa7-69b2-44aa-969a-9f6eef62a617   1Gi        RWO            standard       17d
data-yb-tserver-0        Bound    pvc-8015a1c6-b6af-4692-8abc-1075e8d0e40d   1Gi        RWO            standard       17d
data-yb-tserver-1        Bound    pvc-5fbe39b2-628c-485d-a418-ced1345ca0ec   1Gi        RWO            standard       17d
```

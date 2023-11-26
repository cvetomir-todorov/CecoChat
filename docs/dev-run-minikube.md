# Minikube deployment

Make sure that the [prerequisites](dev-run-prerequisites.md) have been met before continuing.

## Instances

* Integration
  - Kafka - 2 Zookeepers, 2 brokers
* Data storage
  - YugabyteDB - 2 masters, 2 tservers, local pgAdmin is needed for management
  - Cassandra - 2 nodes
  - Redis - 1 instance
* Observability
  - Telemetry - 1 OTel collector
  - Tracing - 1 Jaeger all-in-one
* CecoChat
  - BFF branch - 2 BFF, 2 User, 2 State, 2 History
  - Messaging branch - 2 Messaging, 2 IDGen

## Tools

* `minikube` with the addons listed in `enable-minikube-addons.sh`
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

Initial dynamic config is created manually from inside the Redis pod using the content of the `data.sh` script and lives as long as the same `PersistentVolume` is used:
```shell
kubectl exec -it redis-0 -- bash
```

Initial Kafka topics are created manually from inside one of the Kafka broker pods using the content of the `kafka-topics.sh` script and live as long as the same `PersistentVolume` is used, but beware that successful start of Kafka may take a bit more than the other components, including 1-2 restarts of the pods:
```shell
kubectl exec -it backplane-kafka-0 -- bash
```

The databases in Cassandra and Yugabyte are created when the respective service is started, if they do not exist.

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
* `https://jaeger.cecochat.com` -> Jaeger tracing

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
backplane	cecochat 	1       	2023-11-18 19:57:46.360555795 +0200 EET	deployed	backplane-0.1.0	0.1.0      
bff      	cecochat 	1       	2023-11-18 20:13:12.239367456 +0200 EET	deployed	bff-0.1.0      	0.1.0      
cassandra	cecochat 	1       	2023-11-18 19:57:46.572494357 +0200 EET	deployed	cassandra-0.1.0	0.1.0      
history  	cecochat 	1       	2023-11-18 20:13:13.11653378 +0200 EET 	deployed	history-0.1.0  	0.1.0      
idgen    	cecochat 	1       	2023-11-18 20:13:12.812826459 +0200 EET	deployed	idgen-0.1.0    	0.1.0      
messaging	cecochat 	1       	2023-11-18 20:13:12.57132434 +0200 EET 	deployed	messaging-0.1.0	0.1.0      
redis    	cecochat 	1       	2023-11-18 19:57:46.215992809 +0200 EET	deployed	redis-0.1.0    	0.1.0      
state    	cecochat 	1       	2023-11-18 20:13:12.971157831 +0200 EET	deployed	state-0.1.0    	0.1.0      
telemetry	cecochat 	1       	2023-11-18 20:12:50.424001441 +0200 EET	deployed	telemetry-0.1.0	0.1.0      
tracing  	cecochat 	1       	2023-11-18 20:12:50.597873334 +0200 EET	deployed	tracing-0.1.0  	0.1.0      
user     	cecochat 	1       	2023-11-18 20:13:12.421146806 +0200 EET	deployed	user-0.1.0     	0.1.0      
yb       	cecochat 	1       	2023-11-18 19:57:46.752954723 +0200 EET	deployed	yugabyte-0.1.0 	0.1.0       
```

```shell
$ kubectl get pod
NAME                                        READY   STATUS    RESTARTS      AGE
backplane-kafka-0                           1/1     Running   2 (15m ago)   16m
backplane-kafka-1                           1/1     Running   2 (15m ago)   16m
backplane-zk-0                              1/1     Running   0             16m
backplane-zk-1                              1/1     Running   0             16m
bff-5c779887b9-n9xph                        1/1     Running   0             70s
bff-5c779887b9-vqxmh                        1/1     Running   0             70s
cassandra-0                                 1/1     Running   0             16m
cassandra-1                                 1/1     Running   0             16m
history-5bbfdc7764-89dgq                    1/1     Running   0             69s
history-5bbfdc7764-vqtqv                    1/1     Running   0             69s
idgen-0                                     1/1     Running   0             70s
idgen-1                                     1/1     Running   0             70s
messaging-0                                 1/1     Running   0             70s
messaging-1                                 1/1     Running   0             70s
redis-0                                     1/1     Running   0             16m
redis-1                                     1/1     Running   0             16m
redis-2                                     1/1     Running   0             16m
state-897f6746d-5p5hj                       1/1     Running   0             69s
state-897f6746d-x4qlp                       1/1     Running   0             69s
telemetry-otel-collector-7c67b95b6f-jbh9k   1/1     Running   0             92s
telemetry-otel-collector-7c67b95b6f-z6vqn   1/1     Running   0             92s
tracing-jaeger-0                            1/1     Running   0             92s
user-75f5844f74-hnbg9                       1/1     Running   2 (58s ago)   70s
user-75f5844f74-vsplz                       1/1     Running   0             70s
yb-master-0                                 1/1     Running   0             16m
yb-master-1                                 1/1     Running   0             16m
yb-tserver-0                                1/1     Running   0             16m
yb-tserver-1                                1/1     Running   0             16m
```

```shell
$ kubectl get service
NAME                       TYPE        CLUSTER-IP       EXTERNAL-IP   PORT(S)                       AGE
backplane-kafka            ClusterIP   None             <none>        9092/TCP                      17m
backplane-zk               ClusterIP   None             <none>        2181/TCP,2888/TCP,3888/TCP    17m
bff                        ClusterIP   10.102.32.183    <none>        443/TCP                       113s
cassandra                  ClusterIP   None             <none>        9042/TCP                      17m
history                    ClusterIP   10.97.83.177     <none>        443/TCP                       112s
idgen                      ClusterIP   None             <none>        443/TCP                       113s
messaging                  ClusterIP   None             <none>        443/TCP                       113s
messaging-0                ClusterIP   10.99.205.90     <none>        443/TCP                       113s
messaging-1                ClusterIP   10.107.106.32    <none>        443/TCP                       113s
redis                      ClusterIP   None             <none>        6379/TCP                      17m
state                      ClusterIP   10.98.129.160    <none>        443/TCP                       112s
telemetry-otel-collector   ClusterIP   10.101.152.188   <none>        4317/TCP                      2m15s
tracing-jaeger             ClusterIP   None             <none>        4317/TCP,5778/TCP,16686/TCP   2m15s
user                       ClusterIP   10.103.210.201   <none>        443/TCP                       113s
yb-master                  ClusterIP   None             <none>        7000/TCP,7100/TCP             17m
yb-tserver                 ClusterIP   None             <none>        5433/TCP,9000/TCP,9100/TCP    17m
```

```shell
$ kubectl get deployments
NAME                       READY   UP-TO-DATE   AVAILABLE   AGE
bff                        2/2     2            2           2m33s
history                    2/2     2            2           2m32s
state                      2/2     2            2           2m32s
telemetry-otel-collector   2/2     2            2           2m55s
user                       2/2     2            2           2m33s
```

```shell
$ kubectl get replicasets
NAME                                  DESIRED   CURRENT   READY   AGE
bff-5c779887b9                        2         2         2       3m7s
history-5bbfdc7764                    2         2         2       3m6s
state-897f6746d                       2         2         2       3m6s
telemetry-otel-collector-7c67b95b6f   2         2         2       3m29s
user-75f5844f74                       2         2         2       3m7s
```

```shell
$ kubectl get statefulsets
NAME              READY   AGE
backplane-kafka   2/2     19m
backplane-zk      2/2     19m
cassandra         2/2     19m
idgen             2/2     3m39s
messaging         2/2     3m39s
redis             3/3     19m
tracing-jaeger    1/1     4m1s
yb-master         2/2     19m
yb-tserver        2/2     19m
```

```shell
$ kubectl get ingress
NAME             CLASS   HOSTS                    ADDRESS        PORTS     AGE
bff              nginx   bff.cecochat.com         192.168.49.2   80, 443   4m8s
messaging        nginx   messaging.cecochat.com   192.168.49.2   80, 443   4m8s
tracing-jaeger   nginx   jaeger.cecochat.com      192.168.49.2   80, 443   4m30s
```

```shell
$ kubectl get pvc
NAME                     STATUS   VOLUME                                     CAPACITY   ACCESS MODES   STORAGECLASS   AGE
data-backplane-kafka-0   Bound    pvc-5b72b32b-9309-4ef6-aaca-358c2f7ceff6   512Mi      RWO            standard       21d
data-backplane-kafka-1   Bound    pvc-8a7f3813-bc35-48ae-94ab-f509feb8780f   512Mi      RWO            standard       21d
data-backplane-zk-0      Bound    pvc-31c3bc91-c0d0-49ed-be6f-ea55510918dd   128Mi      RWO            standard       21d
data-backplane-zk-1      Bound    pvc-fa857bb8-293d-433d-a5f2-b14930b88b74   128Mi      RWO            standard       21d
data-cassandra-0         Bound    pvc-af36ee79-e457-4907-af23-c517b04ca99e   1Gi        RWO            standard       21d
data-cassandra-1         Bound    pvc-1ab58971-9842-4fef-9277-6ec94d7715b6   1Gi        RWO            standard       21d
data-redis-0             Bound    pvc-7352a4b6-38a9-4c90-82e9-fe06b56c9e75   128Mi      RWO            standard       26m
data-redis-1             Bound    pvc-9a78bab0-1424-440e-818e-96f28bdce93d   128Mi      RWO            standard       26m
data-redis-2             Bound    pvc-e9429b83-feea-4b85-8d42-973a8038b188   128Mi      RWO            standard       26m
data-yb-master-0         Bound    pvc-75ccf31f-237d-4dde-b314-227737aa930c   1Gi        RWO            standard       21d
data-yb-master-1         Bound    pvc-9acb60f4-e6c4-4962-971e-6838832e74f6   1Gi        RWO            standard       21d
data-yb-tserver-0        Bound    pvc-0c0df721-75a2-42bf-b6fa-54190d017db7   1Gi        RWO            standard       21d
data-yb-tserver-1        Bound    pvc-09e13e35-9a20-43b0-bd34-696df8a2c01a   1Gi        RWO            standard       21d
```

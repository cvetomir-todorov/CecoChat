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
helm install redis redis/
helm install backplane backplane/
helm install cassandra cassandra/
helm install yb yugabyte/
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
helm install jaeger tracing/
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

### Messaging service branch

```shell
helm install idgen idgen/
helm install messaging messaging/
```

### BFF service branch

```shell
helm install user user/
helm install state state/
helm install history history/
helm install bff bff/
```

## Cluster access

The Kubernetes ingresses expose the following services via the respective domains:
* `https://bff.cecochat.com` -> BFF service
* `https://messaging.cecochat.com` -> Messaging service
* `https://jaeger.cecochat.com` -> Jaeger tracing

The command `kubectl port-forward <pod-name> <machine-port>:<pod-port>` could be used to temporarily gain access to some component.

Running `curl https://bff.cecochat.com/healthz -v` could help investigate any issues with the ingress certificate.

## Validate deployment

```shell
$ helm list
NAME     	NAMESPACE	REVISION	UPDATED                                 	STATUS  	CHART          	APP VERSION
backplane	cecochat 	1       	2023-03-30 22:19:42.60321591 +0300 EEST 	deployed	backplane-0.1.0	0.1.0      
bff      	cecochat 	1       	2023-03-30 22:35:06.117783421 +0300 EEST	deployed	bff-0.1.0      	0.1.0      
cassandra	cecochat 	1       	2023-03-30 22:20:08.59761779 +0300 EEST 	deployed	cassandra-0.1.0	0.1.0      
history  	cecochat 	1       	2023-03-30 22:34:23.442475644 +0300 EEST	deployed	history-0.1.0  	0.1.0      
idgen    	cecochat 	1       	2023-03-30 22:32:19.775861257 +0300 EEST	deployed	idgen-0.1.0    	0.1.0      
jaeger   	cecochat 	1       	2023-03-30 22:25:32.363375344 +0300 EEST	deployed	jaeger-0.1.0   	0.1.0      
messaging	cecochat 	1       	2023-03-30 22:32:47.518270136 +0300 EEST	deployed	messaging-0.1.0	0.1.0      
redis    	cecochat 	1       	2023-03-30 22:19:28.213796566 +0300 EEST	deployed	redis-0.1.0    	0.1.0      
state    	cecochat 	1       	2023-03-30 22:34:15.883187975 +0300 EEST	deployed	state-0.1.0    	0.1.0      
user     	cecochat 	1       	2023-03-30 22:33:21.157567525 +0300 EEST	deployed	user-0.1.0     	0.1.0      
yb       	cecochat 	1       	2023-03-30 22:20:36.079955985 +0300 EEST	deployed	yugabyte-0.1.0 	0.1.0 
```

```shell
$ kubectl get pod
NAME                       READY   STATUS    RESTARTS        AGE
backplane-kafka-0          1/1     Running   2 (15m ago)     17m
backplane-kafka-1          1/1     Running   2 (15m ago)     17m
backplane-zk-0             1/1     Running   0               17m
backplane-zk-1             1/1     Running   0               17m
bff-5c779887b9-glppr       1/1     Running   0               109s
bff-5c779887b9-jp75d       1/1     Running   0               109s
cassandra-0                1/1     Running   0               16m
cassandra-1                1/1     Running   0               16m
history-847964847d-m59l2   1/1     Running   0               2m32s
history-847964847d-z9rqd   1/1     Running   1 (2m30s ago)   2m32s
idgen-0                    1/1     Running   0               4m36s
idgen-1                    1/1     Running   0               4m36s
jaeger-0                   1/1     Running   0               11m
messaging-0                1/1     Running   0               4m8s
messaging-1                1/1     Running   0               4m8s
redis-0                    1/1     Running   0               17m
state-688bf6c9c9-bwlh9     1/1     Running   1 (2m38s ago)   2m40s
state-688bf6c9c9-tbwx2     1/1     Running   0               2m40s
user-5589458f67-ns9cj      1/1     Running   0               3m34s
user-5589458f67-vt6sz      1/1     Running   0               3m34s
yb-master-0                1/1     Running   0               16m
yb-master-1                1/1     Running   0               16m
yb-tserver-0               1/1     Running   0               16m
yb-tserver-1               1/1     Running   0               16m
```

```shell
$ kubectl get service
NAME              TYPE        CLUSTER-IP       EXTERNAL-IP   PORT(S)                                                            AGE
backplane-kafka   ClusterIP   None             <none>        9092/TCP                                                           17m
backplane-zk      ClusterIP   None             <none>        2181/TCP,2888/TCP,3888/TCP                                         17m
bff               ClusterIP   10.108.7.193     <none>        443/TCP                                                            109s
cassandra         ClusterIP   None             <none>        9042/TCP                                                           16m
history           ClusterIP   10.104.245.180   <none>        443/TCP                                                            2m32s
idgen             ClusterIP   None             <none>        443/TCP                                                            4m36s
jaeger            ClusterIP   10.108.148.151   <none>        5775/UDP,5778/TCP,6831/UDP,6832/UDP,9411/TCP,14268/TCP,16686/TCP   11m
messaging         ClusterIP   None             <none>        443/TCP                                                            4m8s
messaging-0       ClusterIP   10.111.137.144   <none>        443/TCP                                                            4m8s
messaging-1       ClusterIP   10.100.102.160   <none>        443/TCP                                                            4m8s
redis             ClusterIP   10.105.114.121   <none>        6379/TCP                                                           17m
state             ClusterIP   10.104.184.50    <none>        443/TCP                                                            2m40s
user              ClusterIP   10.110.124.241   <none>        443/TCP                                                            3m34s
yb-master         ClusterIP   None             <none>        7000/TCP,7100/TCP                                                  16m
yb-tserver        ClusterIP   None             <none>        5433/TCP,9000/TCP,9100/TCP                                         16m
```

```shell
$ kubectl get deployments
NAME      READY   UP-TO-DATE   AVAILABLE   AGE
bff       2/2     2            2           3m14s
history   2/2     2            2           3m57s
state     2/2     2            2           4m5s
user      2/2     2            2           4m59s
```

```shell
$ kubectl get replicasets
NAME                 DESIRED   CURRENT   READY   AGE
bff-5c779887b9       2         2         2       3m59s
history-847964847d   2         2         2       4m42s
state-688bf6c9c9     2         2         2       4m50s
user-5589458f67      2         2         2       5m44s
```

```shell
$ kubectl get statefulsets
NAME              READY   AGE
backplane-kafka   2/2     17m
backplane-zk      2/2     17m
cassandra         2/2     16m
idgen             2/2     4m36s
jaeger            1/1     11m
messaging         2/2     4m8s
redis             1/1     17m
yb-master         2/2     16m
yb-tserver        2/2     16m
```

```shell
$ kubectl get ingress
NAME        CLASS   HOSTS                    ADDRESS        PORTS     AGE
bff         nginx   bff.cecochat.com         192.168.49.2   80, 443   5m42s
jaeger      nginx   jaeger.cecochat.com      192.168.49.2   80, 443   15m
messaging   nginx   messaging.cecochat.com   192.168.49.2   80, 443   8m1s
```

```shell
$ kubectl get pvc
data-backplane-kafka-0   Bound    pvc-bb4dbe63-903f-44a5-bfba-5871910c871d   512Mi      RWO            standard       21m
data-backplane-kafka-1   Bound    pvc-d99d7b98-5099-4be4-aa91-e8f60a48fd30   512Mi      RWO            standard       21m
data-backplane-zk-0      Bound    pvc-b33382f0-d8d1-4b92-bb95-bb7fee65c8bb   128Mi      RWO            standard       21m
data-backplane-zk-1      Bound    pvc-55590c71-87ab-4b68-9f33-786b7ae25d3a   128Mi      RWO            standard       21m
data-cassandra-0         Bound    pvc-86943ed7-cae4-4b5b-80dc-5ef077fdbd9d   1Gi        RWO            standard       20m
data-cassandra-1         Bound    pvc-a9dbfb38-54e7-4a89-9ef4-d31fbbe73ce4   1Gi        RWO            standard       20m
data-redis-0             Bound    pvc-5fb2aa58-adbb-4b6f-91a3-6bb52f37c2f4   128Mi      RWO            standard       21m
data-yb-master-0         Bound    pvc-354f848f-d1b8-49c4-bbb7-e7e0176ad8cf   1Gi        RWO            standard       20m
data-yb-master-1         Bound    pvc-0bc2a455-612d-46cc-88d7-5cc72883c24a   1Gi        RWO            standard       20m
data-yb-tserver-0        Bound    pvc-c7d90ddb-757c-4bec-a645-56c9b60995b9   1Gi        RWO            standard       20m
data-yb-tserver-1        Bound    pvc-1d3c8d02-824c-4142-ade6-1cf998caa63f   1Gi        RWO            standard       20m
```

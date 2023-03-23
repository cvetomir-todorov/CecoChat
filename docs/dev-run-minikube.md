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

## Deployment

This is a stepping stone before moving to Helm charts in terms of dependencies. Creating Kubernetes resources could be achieved by:
* `kubectl apply -f <some.yml>` for a single file
* `kubectl apply -f <some-folder>/` for a whole component

Ordered creation steps:
* Namespace
* Security
  - TLS secret for ingress
  - Append domains to `/etc/hosts`
* Parallel 
  - Backplane
  - Yugabyte
  - Cassandra
  - Redis
  - Tracing
* CecoChat service configmaps
* Ordered
  - Parallel
    - User service
    - State service
    - History service
  - BFF service
* Ordered
  - IDGen service
  - Messaging service

Some of the 3rd party components and all of the CecoChat components have health probes. They could be used to ensure the component is running before moving onto the next one. Commands like `kubectl get all`, `kubectl describe pod <pod-name>`, `kubectl logs <pod-name>` could help inspect and troubleshoot a pod.

## Manual setup

**After** running some of the pods they need to be prepared additionally using `kubectl exec -it <pod-name> bash` using the content of the related scripts in the respective folder. This is a one time setup which persists data in the volume. If the volume is recreated it needs to be repeated.

* Integration:
  - Kafka create topics
* Data storage:
  - Redis initial dynamic configuration
* Observability
  - Grafana dashboards import

## Cluster access

The Kubernetes ingresses expose the following services via the respective domains:
* `https://bff.cecochat.com` -> BFF service
* `https://messaging.cecochat.com` -> Messaging service
* `https://jaeger.cecochat.com` -> Jaeger tracing

Additionally `kubectl port-forward <pod-name> <machine-port>:<pod-port>` could be used to temporarily gain access to some component.

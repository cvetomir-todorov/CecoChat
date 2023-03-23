# Run locally

* The whole system contains a lot of components. Despite that everything can be ran locally on a machine powerful enough, mainly one having lots of RAM.
* For local development it is more convenient to use the Docker deployment with a selection of components from the integration, data storage and observability categories, while the .NET services could be ran in the IDE.
* For checking how the system would behave in a production-like Kubernetes cluster and experimenting with the operating aspects, the Minikube deployment is more suitable.

# Certificates

Security is part of modern development with an ever-growing importance. Both the .NET services and the Minikube deployment use TLS certificates. Below are their locations:

* [.NET services certificate](../source/certificates/)
* [Minikube ingress certificate](../deploy/minikube/certificates/)

For a setup follow the steps and use the related scripts:

* The certificates are already created
  - That is fine, since these are just development-purpose certificates, not test/staging/production ones
  - Optionally, they could be created anew using the scripts based on [openssl](https://www.openssl.org/)
  - The password for the .pfx could be seen in the deployments
* The certificates are self-signed and need to be trusted
  - There are scripts for Debian/Ubuntu based systems

# Containerization

* In order to containerize the system you can use the scripts in the [package folder](../package/) in order to build the Docker images.
* The Docker files for the .NET services do `dotnet publish` and use `Release` configuration but this can be changed as preferred.

# Clients

There is only a minimal but functional console client which can be run from either the IDE or the terminal.

# Choose

* [Docker deployment](dev-run-docker.md)
* [Minikube deployment](dev-run-minikube.md)

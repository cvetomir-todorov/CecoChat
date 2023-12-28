# Run locally

* The whole system contains a lot of components, despite that everything can be ran locally on a machine powerful enough, mainly one having lots of RAM
* For local development it is more convenient to use the Docker deployment with a selection of components from the integration, data storage and observability categories, while the .NET services could be ran in the IDE
* For checking how the system would behave in a production-like Kubernetes cluster and experimenting with the operating aspects, the Minikube deployment is more suitable

# Certificates

Security is a part of the modern development with an ever-growing importance. Both the .NET services and the Minikube deployment use TLS certificates.

* [.NET services certificate](../source/certificates)
* [Minikube ingress certificate](../deploy/minikube/certificates)

Security considerations:

* Adding to git the certificates which are self-signed and should be trusted is a security vulnerability
* The certificates are git-ignored and should be generated after the initial clone of the repo
* Everyone should generate their own certificates - that is what happens even during each build

Ubuntu/Debian setup:

* The script `create-certificate.sh` could be used to generate the certificates
* Creating the certificates is achieved via [openssl](https://www.openssl.org/) and its default `.conf` file (which was copied locally, renamed and edited)
* Trusting the certificates is achieved via the `trust-certificate.sh` script

Other systems:

* Other *nix systems and Windows have their own approach of generating certificates and trusting them
* There is an important detail which is required when generating certificates using a different `.conf` file or even a different toolset
* Look for the `[ alt_names ]` section in the respective `.conf` file to know which domains to include for the respective certificate

# Containerization

* In order to containerize the system the scripts in the [package folder](../package) should be used in order to build the Docker images
* The Docker files for the .NET services do `dotnet publish` and use `Release` configuration but this can be changed as preferred
* The shell needs to be pointed to the correct docker daemon, e.g. for Minikube `minikube docker-env` should be applied

# Clients

There is only a minimal but functional console client which can be run from either the IDE or the terminal

# Choose

* [Docker deployment](dev-run-docker.md)
* [Minikube deployment](dev-run-minikube.md)

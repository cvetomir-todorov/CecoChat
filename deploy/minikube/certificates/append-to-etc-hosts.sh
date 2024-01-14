MINIKUBE_IP=$(minikube ip)
echo "$MINIKUBE_IP cecochat.com" | sudo tee --append /etc/hosts
echo "$MINIKUBE_IP bff.cecochat.com" | sudo tee --append /etc/hosts
echo "$MINIKUBE_IP messaging.cecochat.com" | sudo tee --append /etc/hosts
echo "$MINIKUBE_IP jaeger.cecochat.com" | sudo tee --append /etc/hosts
echo "$MINIKUBE_IP kibana.cecochat.com" | sudo tee --append /etc/hosts
echo "$MINIKUBE_IP config.cecochat.com" | sudo tee --append /etc/hosts
echo "$MINIKUBE_IP minio.cecochat.com" | sudo tee --append /etc/hosts

# generate a private key
openssl genrsa -out ingress.key 4096
openssl rsa -text -in ingress.key

# create a CSR using the multi-domain config
openssl req -new -subj "/C=EU/ST=Bulgaria/L=Sofia/O=CecoChat/OU=CecoChat Dev/CN=cecochat.ingress" -key ingress.key -config ingress.conf -out ingress.csr
openssl req -text -in ingress.csr

# create a certificate using the CSR and sign it with the private key
openssl x509 -req -days 3650 -sha512 -in ingress.csr -extensions v3_req -extfile ingress.conf -signkey ingress.key -out ingress.crt
openssl x509 -text -in ingress.crt

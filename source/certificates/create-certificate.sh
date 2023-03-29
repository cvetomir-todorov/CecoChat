# generate a private key
openssl genrsa -out services.key 4096
openssl rsa -text -in services.key

# create a CSR using the multi-domain config
openssl req -new -subj "/C=EU/ST=Bulgaria/L=Sofia/O=CecoChat/OU=CecoChat Dev/CN=cecochat.services" -key services.key -config services.conf -out services.csr
openssl req -text -in services.csr

# create a certificate using the CSR and sign it with the private key
openssl x509 -req -days 3650 -sha512 -in services.csr -extensions v3_req -extfile services.conf -signkey services.key -out services.crt
openssl x509 -text -in services.crt

# create a pfx
cat services.crt services.key > services.all
openssl pkcs12 -export -in services.all -out services.pfx -password pass:cecochat
rm services.all

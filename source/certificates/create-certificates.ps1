$caroot = New-SelfSignedCertificate -DnsName 'ceco.com', 'localhost', 'identity' -FriendlyName 'CA certificate for ceco.com' -NotAfter (Get-Date).AddYears(10)  -KeyLength 2048 -KeyAlgorithm 'RSA' -HashAlgorithm 'SHA256' -KeyUsage CertSign, CRLSign -KeyExportPolicy Exportable -CertStoreLocation 'Cert:\CurrentUser\My'

$webcert = New-SelfSignedCertificate -DnsName 'cecochat.com', 'localhost', 'identity' -FriendlyName 'HTTPS certificate for cecochat' -NotAfter (Get-Date).AddYears(10) -KeyLength 2048 -KeyAlgorithm 'RSA' -HashAlgorithm 'SHA256' -KeyExportPolicy Exportable -CertStoreLocation 'Cert:\CurrentUser\My' -Signer $caroot
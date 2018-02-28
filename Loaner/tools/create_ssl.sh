echo "Generating a private key: openssl genrsa 2048 > private.pem"
openssl genrsa 2048 > private.pem

echo "Generating the self signed certificate: openssl req -x509 -new -key private.pem -out public.pem"
openssl req -x509 -new -key private.pem -out public.pem

echo "If required, creating PFX: openssl pkcs12 -export -in public.pem -inkey private.pem -out mycert.pfx"
openssl pkcs12 -export -in public.pem -inkey private.pem -out mycert.pfx

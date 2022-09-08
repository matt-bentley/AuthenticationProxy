# Ingress Deployment

The nginx ingress controller is used to serve traffic to the namespace.

The controller can be deployed using:
```bash
helm install nginx ingress-nginx --repo https://kubernetes.github.io/ingress-nginx
```

## Generate Certificate Secret

A TLS secret must be created using the self-signed pfx SSL certificate using the following:

```bash
# this must be executed from the Gateway/Certificates folder
# extract encrypted private key from pfx
openssl pkcs12 -in aspnetapp.pfx -nocerts -out aspnetapp.key

# the password for the provided cert is 'password'

# extract the public key
openssl pkcs12 -in aspnetapp.pfx -clcerts -nokeys -out aspnetapp.crt

# decrypt the private key
openssl rsa -in aspnetapp.key -out aspnetapp-decrypted.key
```

Create the kubernetes TLS secret:

```bash
kubectl create secret tls tls-certificate --key="aspnetapp-decrypted.key" --cert="aspnetapp.crt" --dry-run=client -o yaml
```

## Generating a new Certificate

If a new pfx SSL is needed then dotnet dev-certs can be created using the following:
```cmd
dotnet dev-certs https --clean
dotnet dev-certs https -ep %USERPROFILE%\.aspnet\https\aspnetapp.pfx -p password
dotnet dev-certs https --trust
```

The new certificate must be added to the `Gateway` project.
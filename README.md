# Authentication Proxy

This project uses YARP (Yet Another Reverse Proxy) to offload authentication at a gateway proxy so that downstream applications don't need to have any authentication code/logic.

This solution can be used as an API gateway or as a sidecar proxy for Kubernetes pods.

The user's identity claims are passed down to the downstream services as headers.

## Authentication

The gateway uses **OpenId Connect Authorization Code Flow** to challenge/authenticate unauthenticated users. The *offline_access* scope is used to retrieve refresh tokens so that the gateway can refresh access_tokens when they are due to expire.

The gateway can authenticate requests using both Cookies and JWT bearer tokens. The default challenge method is cookies.

### Cookies

Cookies are used to authenticate requests through the gateway by default rather than JWTs. Cookies are used by default for the following reasons:
- Client applications will not need any logic for handling the tokens
- Cookies are stored as `Secure`, `Lax` and `HttpOnly` so that they cannot be retrieved in a XSS attack unlike JWTs which are normally stored in local storage. 

The *access_token* and *refresh_token* is stored in the session cookie so that any Gateway instance can extract and refresh the access_token. This allows the gateway to be load balanced or used as a sidecar container to multiple applications.

The Data Protection key is stored inside the Gateway container so that the same key is used for creating cookies for all instances. In a production system the data protection keys should be rotated and stored securely using a system such as Azure Key Vault.

### JWT Bearer

Any requests that container an `Authorization` header with a **Bearer** token will be authenticated using the JWT token rather than a cookie. This is useful for server-to-server requests.

## Forwarded Claims/Headers

Headers were chosen as the mechanism to pass through the identity information so that no additional configuration is needed for the downstream applications. JWTs could be used for the claims however the downsteam apps would need to know about the issuing authority and how to validate the JWTs.

The claims are serialized into an identity using the ***GatewayAuthenticationHandler***.

The access_token is passed in via the **Authorization** header to downstream requests so that the downstream applications can use it to make additional requests for the user. The access_token is passed downstream regardless of whether Cookies or JWTs are used to authenticate the user.

The following claims are passed through the gateway. If any of these headers are passed in the original request they will be removed by the gateway.

Header Name | Description | Example |
--- | --- |--- |
x-forwarded-name | Username | Test User |
x-forwarded-email | Email | user@dev-28752567-admin.okta.com |
x-forwarded-givenname | First name | Test |
x-forwarded-surname | Last name | User |
Authorization | JWT Bearer token | Bearer {access_token} |

## Build & Run

The solution can be run via Visual Studio or in Kubernetes.

### Run in Visual Studio

To run in Visual Studio the following projects must be set as startup projects:

- Gateway
- Weather.Api
- Weather.Web.Server

This will run a single Gateway instance which can route to either the API or Web application using path based routing.

### Run in Kubernetes

In Kubernetes the Gateway runs as a sidecar container on each of the API and Web pods. An Nginx ingress controller is used as the frontend reverse proxy to offload SSL and route traffic to the API and Web pods.

#### Build Containers

The Docker containers can be built using the following:
```bash
docker-compose build
```

#### Trust Dev Certificate

A self-signed certificate has been generated using dotnet dev-certs. This must be trusted to stop SSL errors appearing in the browser when accessing. Follow these steps if using Windows:
- Double click on `aspnetapp.crt`
- Click 'Install Certificate'
- Select Store Location 'Current User'
- Select 'Place all certificates in the following store' then 'Browse'
- Select 'Trusted Root Certification Authorities'
- Accept the security warning to install

#### Deploy to Kubernetes

The following can be used to install the Kubernetes components after the containers have been built:

```bash
# install nginx ingress
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update
helm install nginx ingress-nginx --repo https://kubernetes.github.io/ingress-nginx

# deploy ingress SSL certificate and ingress routes
kubectl apply -f k8s\ingress\ingress.yml

# deploy api pod
kubectl apply -f k8s\api\api.yml

# deploy web pod
kubectl apply -f k8s\web\web.yml
```

### Application Endpoints

When debugging via Visual Studio the url will be '**https://localhost:9090**' and when accessing via Kubernetes the url will be '**https://localhost**'.

The application can be accessed using the following urls:

Description | Url |
--- | --- |
Blazor Web App | https://localhost:9090 |
API Swagger | https://localhost:9090/swagger |
Retrieve Token | https://localhost:9090/token |
Logout | https://localhost:9090/logout?redirectUrl={your redirect url} |
Show Claims | https://localhost:9090/whoami |
Show Decrypted Session Cookie | https://localhost:9090/session |
Gateway Health | https://localhost:9090/gateway/healthz |
API Health | https://localhost:9090/api/healthz |

### User Credentials

An Okta application has been configured for testing. The following users can be used to sign-in:

Username | Password | Roles |
--- | --- | --- |
user@dev-28752567-admin.okta.com | Pa$$w0rd | Normal user |
admin@dev-28752567-admin.okta.com | Pa$$w0rd | Can the /api/weatherforecast/detailed endpoint as an Admin |
# Authentication Proxy

This project uses YARP (Yet Another Reverse Proxy) to offload authentication at a gateway proxy so that downstream applications don't need to have any authentication code/logic.

Claims are passed down to the downstream services as headers.

## Authentication

The gateway uses **OpenId Connect Authorization Code Flow** to challenge/authenticate unauthenticated users. The *offline_access* scope is used to retrieve refresh tokens so that the gateway can refresh access_tokens when they are due to expire.

The gateway can authenticate requests using both Cookies and JWT bearer tokens. The default challenge method is cookies.

### Cookies

Cookies are used to authenticate requests through the gateway by default rather than JWTs. Cookies are used by default for the following reasons:
- Client applications will not need any logic for handling the tokens
- Cookies are stored as `Secure`, `Lax` and `HttpOnly` so that they cannot be retrieved in a XSS attack unlike JWTs which are normally stored in local storage. 

The *access_token* and *refresh_token* is stored in the session cookie so that any Gateway instance can extract and refresh the access_token. This allows the gateway to be load balanced or used as a sidecar container to multiple applications.

### JWT Bearer

Any requests that container an `Authorization` header with a **Bearer** token will be authenticated using the JWT token rather than a cookie. This is useful for server-to-server requests.

## Forwarded Claims/Headers

Headers were chosen as the mechanism to pass through the identity information so that no additional configuration is needed for the downstream applications. JWTs could be used for the claims however the downsteam apps would need to know about the issuing authority and how to validate the JWTs.

The claims are serialized into an identity using the ***GatewayAuthenticationHandler***.

The access_token is passed in via the **Authorization** header to downstream requests so that the downstream applications be use it to make additional requests for the user. The access_token is passed downstream regardless of whether Cookies or JWTs are used to authenticate the user.

Header Name | Description | Example |
--- | --- |
x-forwarded-name | Username | Test User |
x-forwarded-email | Email | user@dev-28752567-admin.okta.com |
x-forwarded-givenname | First name | Test |
x-forwarded-surname | Last name | User |
Authorization | JWT Bearer token | Bearer {access_token} |

## Build & Run

The solution can be run via Visual Studio or in Docker containers using docker-compose.

### Trust Dev Certificate

A self-signed certificate has been generated using dotnet dev-certs. This must be trusted so that there are no SSL errors when running the solution. Follow these steps if using Windows:
- Double click on `aspnetapp.crt`
- Click 'Install Certificate'
- Select Store Location 'Current User'
- Select 'Place all certificates in the following store' then 'Browse'
- Select 'Trusted Root Certification Authorities'
- Accept the security warning to install

In production certificates should be loaded in a more secure way or offloaded to another reverse proxy/load balancer.

A new development certificate can be created using the following:
```cmd
dotnet dev-certs https --clean
dotnet dev-certs https -ep %USERPROFILE%\.aspnet\https\aspnetapp.pfx -p password
dotnet dev-certs https --trust
```

The new certificate must be added to the `Gateway` project.

### Run in Visual Studio

To run in Visual Studio the following projects must be set as startup projects:

- Gateway
- Weather.Api
- Weather.Web.Server

### Run in Docker

The Docker containers can be built using the following:
```bash
docker-compose build
```

Run Docker containers:
```bash
docker-compose up
```

### Application Endpoints

The application can be accessed using the following urls:

Description | Url |
--- | --- |
Blazor Web App | https://localhost:9090 |
API Swagger | https://localhost:9090/swagger |
Retrieve Token | https://localhost:9090/token |
Logout | https://localhost:9090/logout?redirectUrl={your redirect url} |
Show Claims | https://localhost:9090/whoami |
Show Decrypted Session Cookie | https://localhost:9090/session |

### User Credentials

An Okta application has been configured for testing. The following users can be used to sign-in:

Username | Password | Roles |
--- | --- | --- |
user@dev-28752567-admin.okta.com | Pa$$w0rd | Normal user |
admin@dev-28752567-admin.okta.com | Pa$$w0rd | Can the /api/weatherforecast/detailed endpoint as an Admin |